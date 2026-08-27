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
            prefix: new HarmonyMethod(typeof(NtrBlockDisplayPatch), nameof(NtrBlockDisplayPatch.Prefix)),
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
            InteractionThumbnailLocalUnlockPatch.TargetUserDataMethod(),
            prefix: new HarmonyMethod(typeof(InteractionThumbnailLocalUnlockPatch), nameof(InteractionThumbnailLocalUnlockPatch.PrefixUserData)));

        _harmony.Patch(
            InteractionThumbnailLocalUnlockPatch.TargetMasterDataMethod(),
            prefix: new HarmonyMethod(typeof(InteractionThumbnailLocalUnlockPatch), nameof(InteractionThumbnailLocalUnlockPatch.PrefixMasterData)));

        _harmony.Patch(
            NtrSceneEntryPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(NtrSceneEntryPatch), nameof(NtrSceneEntryPatch.Prefix)),
            finalizer: new HarmonyMethod(typeof(NtrSceneEntryPatch), nameof(NtrSceneEntryPatch.Finalizer)));

        _harmony.Patch(
            CharacterAbilityLocalViewPatch.TargetModelFactoryMethod(),
            prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixModelFactory)),
            postfix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PostfixModelFactory)));

        _harmony.Patch(
            CharacterAbilityLocalViewPatch.TargetReadOnlyViewMethod(),
            prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixRestoreTrackedButtons)),
            postfix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PostfixReadOnlyView)));

        _harmony.Patch(
            CharacterAbilityLocalViewPatch.TargetAbilityUpCommandMethod(),
            prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixAbilityUpCommand)));

        _harmony.Patch(
            CharacterAbilityLocalViewPatch.TargetUnlockCommandMethod(),
            prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixUnlockCommand)));

        _harmony.Patch(
            PersonalStoryUnlockPatch.TargetMethod(),
            prefix: new HarmonyMethod(typeof(PersonalStoryUnlockPatch), nameof(PersonalStoryUnlockPatch.Prefix)));

        _harmony.Patch(
            ProfileReplayUnlockPatch.TargetMethod(),
            postfix: new HarmonyMethod(typeof(ProfileReplayUnlockPatch), nameof(ProfileReplayUnlockPatch.Postfix)));

        _harmony.Patch(
            SkinChangePopupLocalPatch.TargetInitializePopupMethod(),
            prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixInitializePopup)),
            finalizer: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.FinalizeInitializePopup)));

        _harmony.Patch(
            SkinChangePopupLocalPatch.TargetCreateCharacterSkinModelsMethod(),
            prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixCreateCharacterSkinModels)),
            postfix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PostfixCreateCharacterSkinModels)),
            finalizer: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.FinalizeCreateCharacterSkinModels)));

        _harmony.Patch(
            SkinChangePopupLocalPatch.TargetCreateWeaponSkinModelsMethod(),
            prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixCreateWeaponSkinModels)),
            postfix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PostfixCreateWeaponSkinModels)),
            finalizer: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.FinalizeCreateWeaponSkinModels)));

        _harmony.Patch(
            SkinChangePopupLocalPatch.TargetConfirmSubscriptionMethod(),
            prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixConfirmSubscription)));

        _harmony.Patch(
            SkinChangePopupLocalPatch.TargetConfirmMethod(),
            prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixConfirm)));

        CgUnlockPlugin.LogSource.LogInfo("[CGUnlock] 补丁注册完成：StoryListUnlock / NtrBlockDisplay / NovelReadBypass / CharacterTopRefreshStateMachine / CharacterModelMasterThumbnail / CharacterDataStoreLocalLookup / InteractionMasterThumbnail / UnownedCharacterDetail / NtrSceneEntry / UnownedSkillMasterView / PersonalStoryUnlock / ProfileReplayUnlock / LocalCharacterSkinChange");
    }
}
