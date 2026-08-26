using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// 显式注册全部补丁。编译器闭包目标使用 Il2CppInterop 代理的公开改写名称，
/// 标识符与代理程序集及 ISIL 证据逐字一致 —— 见 evidence/fixtures/proxy-targets.tsv。
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

        _harmony.Patch(
            CharacterTopLocalUnlockPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(CharacterTopLocalUnlockPatch), nameof(CharacterTopLocalUnlockPatch.Prefix)),
            finalizer: new HarmonyMethod(typeof(CharacterTopLocalUnlockPatch), nameof(CharacterTopLocalUnlockPatch.Finalizer)));

        _harmony.Patch(
            CharacterTopListRefreshLocalUnlockPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(CharacterTopListRefreshLocalUnlockPatch), nameof(CharacterTopListRefreshLocalUnlockPatch.Prefix)),
            finalizer: new HarmonyMethod(typeof(CharacterTopListRefreshLocalUnlockPatch), nameof(CharacterTopListRefreshLocalUnlockPatch.Finalizer)));

        _harmony.Patch(
            CharacterModelLocalUnlockPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(CharacterModelLocalUnlockPatch), nameof(CharacterModelLocalUnlockPatch.Prefix)));

        _harmony.Patch(
            CharacterCatalogCapturePatch.TargetMethod(),
            postfix: new HarmonyMethod(typeof(CharacterCatalogCapturePatch), nameof(CharacterCatalogCapturePatch.Postfix)));

        _harmony.Patch(
            UnownedCharacterSelectionPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(UnownedCharacterSelectionPatch), nameof(UnownedCharacterSelectionPatch.Prefix)));

        _harmony.Patch(
            CharacterDetailLocalUnlockPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(CharacterDetailLocalUnlockPatch), nameof(CharacterDetailLocalUnlockPatch.Prefix)),
            finalizer: new HarmonyMethod(typeof(CharacterDetailLocalUnlockPatch), nameof(CharacterDetailLocalUnlockPatch.Finalizer)));

        _harmony.Patch(
            CharacterDataStoreLocalLookupPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(CharacterDataStoreLocalLookupPatch), nameof(CharacterDataStoreLocalLookupPatch.Prefix)));

        _harmony.Patch(
            NtrSceneEntryPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(NtrSceneEntryPatch), nameof(NtrSceneEntryPatch.Prefix)),
            finalizer: new HarmonyMethod(typeof(NtrSceneEntryPatch), nameof(NtrSceneEntryPatch.Finalizer)));

        CgUnlockPlugin.LogSource.LogInfo("[CGUnlock] 补丁注册完成：StoryListUnlock / NtrBlockDisplay / NovelReadBypass / CharacterTopRefreshStateMachine / CharacterModelMasterThumbnail / CharacterDataStoreLocalLookup / UnownedCharacterDetail / NtrSceneEntry");
    }
}
