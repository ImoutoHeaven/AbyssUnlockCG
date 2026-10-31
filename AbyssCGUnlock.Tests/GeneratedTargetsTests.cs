using System;
using System.Reflection;
using AbyssCGUnlock.Patches;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// Compiler-generated type names carry a per-type index that the game renumbers on every update, so the
/// resolvers must key on the method name and signature only. These fixtures mirror the two shapes that
/// broke on 2026-10-30: an async state machine (&lt;UpdateView&gt;d__N) and a closure lambda
/// (&lt;&gt;c__DisplayClassN_0.&lt;InitializeViewAsync&gt;b__6).
/// </summary>
public sealed class GeneratedTargetsTests
{
    private static class SingleStateMachine
    {
        // ReSharper disable once UnusedMember.Local
        private sealed class _UpdateView_d__104
        {
            public void MoveNext()
            {
            }
        }
    }

    private static class NoStateMachine
    {
        // ReSharper disable once UnusedMember.Local
        private sealed class _UpdateOther_d__7
        {
            public void MoveNext()
            {
            }
        }
    }

    private static class AmbiguousStateMachine
    {
        // ReSharper disable once UnusedMember.Local
        private sealed class _UpdateView_d__101
        {
            public void MoveNext()
            {
            }
        }

        // ReSharper disable once UnusedMember.Local
        private sealed class _UpdateView_d__104
        {
            public void MoveNext()
            {
            }
        }
    }

    private static class SingleClosure
    {
        // ReSharper disable once UnusedMember.Local
        private sealed class __c__DisplayClass91_0
        {
            public void _InitializeViewAsync_b__6(string _, int service, object view)
            {
            }
        }
    }

    private static class NestedClosure
    {
        // ReSharper disable once UnusedMember.Local
        private sealed class __c__DisplayClass91_1
        {
            // ReSharper disable once UnusedMember.Local
            private sealed class __c__DisplayClass91_2
            {
                public void _InitializeViewAsync_b__6(string _, int service, object view)
                {
                }
            }
        }
    }

    private static class WrongSignatureClosure
    {
        // ReSharper disable once UnusedMember.Local
        private sealed class __c__DisplayClass91_0
        {
            public void _InitializeViewAsync_b__6(string _)
            {
            }
        }
    }

    private static readonly Type[] Signature = { typeof(string), typeof(int), typeof(object) };

    [Fact]
    public void FindStateMachine_resolves_the_renumbered_type_by_method_name()
    {
        var resolved = GeneratedTargets.FindStateMachine(typeof(SingleStateMachine), "UpdateView");

        Assert.Equal("_UpdateView_d__104", resolved.Name);
    }

    [Fact]
    public void FindStateMachine_fails_closed_when_the_async_method_is_gone()
    {
        var exception = Assert.Throws<MissingMemberException>(
            () => GeneratedTargets.FindStateMachine(typeof(NoStateMachine), "UpdateView"));

        Assert.Contains("_UpdateView_d__", exception.Message);
    }

    [Fact]
    public void FindStateMachine_fails_closed_when_two_generations_coexist()
    {
        var exception = Assert.Throws<MissingMemberException>(
            () => GeneratedTargets.FindStateMachine(typeof(AmbiguousStateMachine), "UpdateView"));

        Assert.Contains("Ambiguous", exception.Message);
    }

    [Fact]
    public void FindClosureMethod_resolves_the_lambda_inside_its_display_class()
    {
        var resolved = GeneratedTargets.FindClosureMethod(
            typeof(SingleClosure),
            "_InitializeViewAsync_b__6",
            Signature);

        Assert.Equal("_InitializeViewAsync_b__6", resolved.Name);
        Assert.Equal("__c__DisplayClass91_0", resolved.DeclaringType!.Name);
    }

    [Fact]
    public void FindClosureMethod_reaches_display_classes_nested_in_display_classes()
    {
        var resolved = GeneratedTargets.FindClosureMethod(
            typeof(NestedClosure),
            "_InitializeViewAsync_b__6",
            Signature);

        Assert.Equal("__c__DisplayClass91_2", resolved.DeclaringType!.Name);
    }

    [Fact]
    public void FindClosureMethod_fails_closed_when_the_signature_drifted()
    {
        var exception = Assert.Throws<MissingMemberException>(
            () => GeneratedTargets.FindClosureMethod(
                typeof(WrongSignatureClosure),
                "_InitializeViewAsync_b__6",
                Signature));

        Assert.Contains("_InitializeViewAsync_b__6", exception.Message);
    }
}
