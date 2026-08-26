using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// 显式注册两个补丁（v0.1.1：不再补编译器闭包方法，目标均为公开方法，
/// 标识符与代理程序集元数据逐字一致 —— 见 evidence/fixtures/proxy-targets.tsv）。
/// </summary>
internal static class PatchManager
{
    private static Harmony _harmony = null!;

    internal static void Initialize()
    {
        _harmony = new Harmony(PluginInfo.PluginGuid);

        _harmony.Patch(
            StoryListUnlockPatch.TargetMethod(),
            postfix: new HarmonyMethod(typeof(StoryListUnlockPatch), nameof(StoryListUnlockPatch.Postfix)));

        _harmony.Patch(
            NtrBlockDisplayPatch.TargetMethod(),
            postfix: new HarmonyMethod(typeof(NtrBlockDisplayPatch), nameof(NtrBlockDisplayPatch.Postfix)));

        _harmony.Patch(
            NovelReadBypassPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(NovelReadBypassPatch), nameof(NovelReadBypassPatch.Prefix)));

        CgUnlockPlugin.LogSource.LogInfo("[CGUnlock] 补丁注册完成：StoryListUnlock / NtrBlockDisplay / NovelReadBypass");
    }
}
