using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AbyssCGUnlock.Patches;
using HarmonyLib;
using Xunit;

namespace AbyssCGUnlock.Tests;

public sealed class PatchPreflightTests
{
    [Fact]
    public void Preflight_plan_collects_missing_and_throwing_targets_without_installing_any()
    {
        var installCount = 0;
        var target = typeof(string).GetMethod(nameof(ToString), Type.EmptyTypes)!;
        var plan = PatchManager.CheckTargetResolvers(new[]
        {
            new PatchManager.PatchRegistration("valid", () => target, (_, _) => installCount++),
            new PatchManager.PatchRegistration("missing", () => null!, (_, _) => installCount++),
            new PatchManager.PatchRegistration(
                "throwing",
                () => throw new MissingMethodException("proxy drift"),
                (_, _) => installCount++),
        });

        Assert.Equal(3, plan.TargetCount);
        Assert.Equal(
            new[]
            {
                "missing => missing-target",
                "throwing => precheck-exception:MissingMethodException:proxy drift",
            },
            plan.Failures);

        var exception = Assert.Throws<InvalidOperationException>(() => PatchManager.Initialize(plan));

        Assert.Contains("failed compatibility precheck", exception.Message);
        Assert.Single(plan.Installers);
        Assert.Equal(0, installCount);
    }

    [Fact]
    public void Installer_failure_rolls_back_prior_installers_and_stops_later_installers()
    {
        var events = new List<string>();
        var installers = new Action<Harmony>[]
        {
            _ => events.Add("first"),
            _ => throw new InvalidOperationException("installer failed"),
            _ => events.Add("later"),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PatchManager.ApplyInstallersWithRollback(installers, null!, () => events.Add("rollback")));

        Assert.Equal("installer failed", exception.Message);
        Assert.Equal(new[] { "first", "rollback" }, events);
    }

    [Fact]
    public void Preflight_plan_installs_the_method_that_was_checked()
    {
        var target = typeof(string).GetMethod(nameof(ToString), Type.EmptyTypes)!;
        MethodBase? installedTarget = null;
        var plan = PatchManager.CheckTargetResolvers(new[]
        {
            new PatchManager.PatchRegistration("String.ToString", () => target, (_, resolved) => installedTarget = resolved),
        });

        Assert.Empty(plan.Failures);
        Assert.Single(plan.Installers);
        plan.Installers[0](null!);
        Assert.Same(target, installedTarget);
    }

    [Fact]
    public void Preflight_resolves_every_current_patch_target()
    {
        RegisterGameProxyResolution();

        var plan = PatchManager.PreflightTargets();

        Assert.Equal(26, plan.TargetCount);
        Assert.Empty(plan.Failures);
        Assert.Equal(26, plan.Installers.Count);
    }

    /// <summary>
    /// Source-shape smoke check: Plugin.Load must bind logging, preflight, log failures, abort,
    /// and only then initialize config and patch installation. Runtime load is exercised in game.
    /// </summary>
    [Fact]
    public void Plugin_source_orders_preflight_gating_before_config_and_install()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "AbyssCGUnlock", "Plugin.cs"));
        var logger = source.IndexOf("LogSource = Log;", StringComparison.Ordinal);
        var preflight = source.IndexOf("PatchManager.PreflightTargets()", StringComparison.Ordinal);
        Assert.True(preflight >= 0, "Plugin.Load must run the target preflight.");

        var failureLog = source.IndexOf("LogSource.LogError", preflight, StringComparison.Ordinal);
        var abort = source.IndexOf("throw new InvalidOperationException", preflight, StringComparison.Ordinal);
        var config = source.IndexOf("PluginConfig.Initialize(this)", StringComparison.Ordinal);
        var patchInstall = source.IndexOf("PatchManager.Initialize(preflight)", StringComparison.Ordinal);

        Assert.True(logger >= 0 && logger < preflight, "Plugin.Load must bind logging before preflight.");
        Assert.True(preflight >= 0 && preflight < failureLog && failureLog < abort);
        Assert.True(abort < config && abort < patchInstall);
    }

    private static string FindRepositoryRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("ABYSS_CGUNLOCK_SOURCE"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
        };
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            for (var directory = new DirectoryInfo(candidate); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "AbyssCGUnlock.sln")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("CGUnlock repository root not found.");
    }

    private static void RegisterGameProxyResolution()
    {
        var gameDir = Environment.GetEnvironmentVariable("ABYSS_GAME_DIR");
        if (string.IsNullOrWhiteSpace(gameDir))
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "dotabyss_x_cl");
                if (Directory.Exists(candidate))
                {
                    gameDir = candidate;
                    break;
                }
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(gameDir), "Set ABYSS_GAME_DIR to the read-only game directory.");
        var assemblyDirectories = new[]
        {
            Path.Combine(gameDir!, "BepInEx", "core"),
            Path.Combine(gameDir!, "BepInEx", "interop"),
        };
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            if (string.IsNullOrWhiteSpace(name.Name))
            {
                return null;
            }

            foreach (var directory in assemblyDirectories)
            {
                var path = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(path))
                {
                    return context.LoadFromAssemblyPath(path);
                }
            }

            return null;
        };
    }
}
