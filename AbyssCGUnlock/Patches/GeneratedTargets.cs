using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Resolves compiler-generated IL2CPP members by shape instead of by generated index.
///
/// The C# compiler numbers state machines and closure classes per declaring type
/// (<c>&lt;UpdateView&gt;d__101</c>, <c>&lt;&gt;c__DisplayClass90_0</c>). Il2CppInterop flattens the
/// angle brackets to underscores. A game update that adds one async method or one closure earlier in
/// the type shifts every later index, so a hardcoded name resolves to nothing and the fail-closed
/// precheck refuses to load the plugin. The method name inside the generated name never drifts, so
/// that is what these resolvers key on.
/// </summary>
internal static class GeneratedTargets
{
    private const BindingFlags Nested = BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>Resolves <c>&lt;MethodName&gt;d__N</c> (interop: <c>_MethodName_d__N</c>) under <paramref name="outer"/>.</summary>
    internal static Type FindStateMachine(Type outer, string methodName)
    {
        var prefix = "_" + methodName + "_d__";
        var matches = Walk(outer)
            .Where(type => IsIndexedGeneratedName(type.Name, prefix))
            .ToArray();

        if (matches.Length == 1)
        {
            return matches[0];
        }

        throw new MissingMemberException(
            matches.Length == 0
                ? $"No state machine '{prefix}N' found under {outer.FullName}; the async method was renamed or removed."
                : $"Ambiguous state machine '{prefix}N' under {outer.FullName}: {string.Join(", ", matches.Select(type => type.FullName))}.");
    }

    /// <summary>
    /// Resolves a lambda method such as <c>_InitializeViewAsync_b__6</c> with the exact signature,
    /// wherever the compiler nested its closure class. Compiler-generated holders only: the declaring
    /// type itself (lambdas that capture <c>this</c> stay on the type) and its closure classes
    /// (<c>&lt;&gt;c</c>, <c>&lt;&gt;c__DisplayClassN_M</c>, <c>&lt;&gt;c__N</c>).
    /// </summary>
    internal static MethodBase FindClosureMethod(Type outer, string lambdaName, Type[] parameters)
    {
        var matches = new List<(Type Holder, MethodBase Method)>();
        foreach (var type in Walk(outer))
        {
            if (!IsGeneratedHolder(type, outer))
            {
                continue;
            }

            MethodBase method;
            try
            {
                method = AccessTools.Method(type, lambdaName, parameters);
            }
            catch (Exception exception) when (exception is TypeLoadException or ArgumentException)
            {
                continue;
            }

            if (method != null)
            {
                matches.Add((type, method));
            }
        }

        if (matches.Count == 1)
        {
            return matches[0].Method;
        }

        throw new MissingMemberException(
            matches.Count == 0
                ? $"No closure method '{lambdaName}({Describe(parameters)})' found under {outer.FullName}; the callback was renamed, re-signatured or removed."
                : $"Ambiguous closure method '{lambdaName}' under {outer.FullName}: {string.Join(", ", matches.Select(match => match.Holder.FullName))}.");
    }

    private static bool IsGeneratedHolder(Type type, Type outer) =>
        type == outer ||
        type.Name == "__c" ||
        type.Name.StartsWith("__c__", StringComparison.Ordinal);

    private static bool IsIndexedGeneratedName(string name, string prefix)
    {
        if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length)
        {
            return false;
        }

        for (var i = prefix.Length; i < name.Length; i++)
        {
            if (name[i] < '0' || name[i] > '9')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Breadth-first walk of the declaring type and its nested types, so lambdas emitted on the type
    /// itself and closures nested in closures are both reachable. An unloadable nested type is skipped
    /// instead of aborting the walk: one broken proxy type must not take the whole plugin down.
    /// </summary>
    private static IEnumerable<Type> Walk(Type outer)
    {
        var queue = new Queue<Type>();
        var seen = new HashSet<Type>();
        queue.Enqueue(outer);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current))
            {
                continue;
            }

            yield return current;

            Type[] children;
            try
            {
                children = current.GetNestedTypes(Nested);
            }
            catch (Exception exception) when (exception is TypeLoadException or NotSupportedException)
            {
                continue;
            }

            foreach (var child in children)
            {
                queue.Enqueue(child);
            }
        }
    }

    private static string Describe(IEnumerable<Type> parameters) =>
        string.Join(", ", parameters.Select(parameter => parameter.Name));
}
