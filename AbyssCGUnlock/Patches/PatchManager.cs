using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Defines every target and its installer once so startup validation and patch application share the same contract.
/// </summary>
internal static class PatchManager
{
    internal sealed record PatchRegistration(
        string Name,
        Func<MethodBase> Resolve,
        Action<Harmony, MethodBase> Install);

    internal sealed record PreflightResult(
        int TargetCount,
        IReadOnlyList<string> Failures,
        IReadOnlyList<Action<Harmony>> Installers);

    private static PatchRegistration[] GetRegistrations() => new[]
    {
        new PatchRegistration(
            nameof(StoryListUnlockPatch) + "." + nameof(StoryListUnlockPatch.TargetMethod),
            StoryListUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                postfix: new HarmonyMethod(typeof(StoryListUnlockPatch), nameof(StoryListUnlockPatch.Postfix)))),
        new PatchRegistration(
            nameof(NtrBlockDisplayPatch) + "." + nameof(NtrBlockDisplayPatch.TargetMethod),
            NtrBlockDisplayPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(NtrBlockDisplayPatch), nameof(NtrBlockDisplayPatch.Prefix)),
                postfix: new HarmonyMethod(typeof(NtrBlockDisplayPatch), nameof(NtrBlockDisplayPatch.Postfix)))),
        new PatchRegistration(
            nameof(NovelReadBypassPatch) + "." + nameof(NovelReadBypassPatch.TargetMethod),
            NovelReadBypassPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(NovelReadBypassPatch), nameof(NovelReadBypassPatch.Prefix)))),
        new PatchRegistration(
            nameof(CharacterTopLocalUnlockPatch) + "." + nameof(CharacterTopLocalUnlockPatch.TargetMethod),
            CharacterTopLocalUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterTopLocalUnlockPatch), nameof(CharacterTopLocalUnlockPatch.Prefix)),
                finalizer: new HarmonyMethod(typeof(CharacterTopLocalUnlockPatch), nameof(CharacterTopLocalUnlockPatch.Finalizer)))),
        new PatchRegistration(
            nameof(CharacterTopListRefreshLocalUnlockPatch) + "." + nameof(CharacterTopListRefreshLocalUnlockPatch.TargetMethod),
            CharacterTopListRefreshLocalUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterTopListRefreshLocalUnlockPatch), nameof(CharacterTopListRefreshLocalUnlockPatch.Prefix)),
                finalizer: new HarmonyMethod(typeof(CharacterTopListRefreshLocalUnlockPatch), nameof(CharacterTopListRefreshLocalUnlockPatch.Finalizer)))),
        new PatchRegistration(
            nameof(CharacterModelLocalUnlockPatch) + "." + nameof(CharacterModelLocalUnlockPatch.TargetMethod),
            CharacterModelLocalUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterModelLocalUnlockPatch), nameof(CharacterModelLocalUnlockPatch.Prefix)))),
        new PatchRegistration(
            nameof(CharacterCatalogCapturePatch) + "." + nameof(CharacterCatalogCapturePatch.TargetMethod),
            CharacterCatalogCapturePatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                postfix: new HarmonyMethod(typeof(CharacterCatalogCapturePatch), nameof(CharacterCatalogCapturePatch.Postfix)))),
        new PatchRegistration(
            nameof(UnownedCharacterSelectionPatch) + "." + nameof(UnownedCharacterSelectionPatch.TargetMethod),
            UnownedCharacterSelectionPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(UnownedCharacterSelectionPatch), nameof(UnownedCharacterSelectionPatch.Prefix)))),
        new PatchRegistration(
            nameof(CharacterDetailLocalUnlockPatch) + "." + nameof(CharacterDetailLocalUnlockPatch.TargetMethod),
            CharacterDetailLocalUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterDetailLocalUnlockPatch), nameof(CharacterDetailLocalUnlockPatch.Prefix)),
                finalizer: new HarmonyMethod(typeof(CharacterDetailLocalUnlockPatch), nameof(CharacterDetailLocalUnlockPatch.Finalizer)))),
        new PatchRegistration(
            nameof(CharacterDataStoreLocalLookupPatch) + "." + nameof(CharacterDataStoreLocalLookupPatch.TargetMethod),
            CharacterDataStoreLocalLookupPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterDataStoreLocalLookupPatch), nameof(CharacterDataStoreLocalLookupPatch.Prefix)))),
        new PatchRegistration(
            nameof(InteractionThumbnailLocalUnlockPatch) + "." + nameof(InteractionThumbnailLocalUnlockPatch.TargetUserDataMethod),
            InteractionThumbnailLocalUnlockPatch.TargetUserDataMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(InteractionThumbnailLocalUnlockPatch), nameof(InteractionThumbnailLocalUnlockPatch.PrefixUserData)))),
        new PatchRegistration(
            nameof(InteractionThumbnailLocalUnlockPatch) + "." + nameof(InteractionThumbnailLocalUnlockPatch.TargetMasterDataMethod),
            InteractionThumbnailLocalUnlockPatch.TargetMasterDataMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(InteractionThumbnailLocalUnlockPatch), nameof(InteractionThumbnailLocalUnlockPatch.PrefixMasterData)))),
        new PatchRegistration(
            nameof(NtrSceneEntryPatch) + "." + nameof(NtrSceneEntryPatch.TargetMethod),
            NtrSceneEntryPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(NtrSceneEntryPatch), nameof(NtrSceneEntryPatch.Prefix)),
                finalizer: new HarmonyMethod(typeof(NtrSceneEntryPatch), nameof(NtrSceneEntryPatch.Finalizer)))),
        new PatchRegistration(
            nameof(CharacterAbilityLocalViewPatch) + "." + nameof(CharacterAbilityLocalViewPatch.TargetModelFactoryMethod),
            CharacterAbilityLocalViewPatch.TargetModelFactoryMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixModelFactory)),
                postfix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PostfixModelFactory)))),
        new PatchRegistration(
            nameof(CharacterAbilityLocalViewPatch) + "." + nameof(CharacterAbilityLocalViewPatch.TargetReadOnlyViewMethod),
            CharacterAbilityLocalViewPatch.TargetReadOnlyViewMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixRestoreTrackedButtons)),
                postfix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PostfixReadOnlyView)))),
        new PatchRegistration(
            nameof(CharacterAbilityLocalViewPatch) + "." + nameof(CharacterAbilityLocalViewPatch.TargetAbilityUpCommandMethod),
            CharacterAbilityLocalViewPatch.TargetAbilityUpCommandMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixAbilityUpCommand)))),
        new PatchRegistration(
            nameof(CharacterAbilityLocalViewPatch) + "." + nameof(CharacterAbilityLocalViewPatch.TargetUnlockCommandMethod),
            CharacterAbilityLocalViewPatch.TargetUnlockCommandMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixUnlockCommand)))),
        new PatchRegistration(
            nameof(CharacterAbilityLocalViewPatch) + "." + nameof(CharacterAbilityLocalViewPatch.TargetExpectedAbilityMethod),
            CharacterAbilityLocalViewPatch.TargetExpectedAbilityMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixCreateExpectedAbility)))),
        new PatchRegistration(
            nameof(CharacterAbilityLocalViewPatch) + "." + nameof(CharacterAbilityLocalViewPatch.TargetInformationViewMethod),
            CharacterAbilityLocalViewPatch.TargetInformationViewMethod,
            static (harmony, target) => harmony.Patch(
                target,
                postfix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PostfixInformationView)))),
        new PatchRegistration(
            nameof(PersonalStoryUnlockPatch) + "." + nameof(PersonalStoryUnlockPatch.TargetMethod),
            PersonalStoryUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(PersonalStoryUnlockPatch), nameof(PersonalStoryUnlockPatch.Prefix)))),
        new PatchRegistration(
            nameof(ProfileReplayUnlockPatch) + "." + nameof(ProfileReplayUnlockPatch.TargetMethod),
            ProfileReplayUnlockPatch.TargetMethod,
            static (harmony, target) => harmony.Patch(
                target,
                postfix: new HarmonyMethod(typeof(ProfileReplayUnlockPatch), nameof(ProfileReplayUnlockPatch.Postfix)))),
        new PatchRegistration(
            nameof(SkinChangePopupLocalPatch) + "." + nameof(SkinChangePopupLocalPatch.TargetInitializePopupMethod),
            SkinChangePopupLocalPatch.TargetInitializePopupMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixInitializePopup)),
                finalizer: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.FinalizeInitializePopup)))),
        new PatchRegistration(
            nameof(SkinChangePopupLocalPatch) + "." + nameof(SkinChangePopupLocalPatch.TargetCreateCharacterSkinModelsMethod),
            SkinChangePopupLocalPatch.TargetCreateCharacterSkinModelsMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixCreateCharacterSkinModels)),
                postfix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PostfixCreateCharacterSkinModels)),
                finalizer: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.FinalizeCreateCharacterSkinModels)))),
        new PatchRegistration(
            nameof(SkinChangePopupLocalPatch) + "." + nameof(SkinChangePopupLocalPatch.TargetCreateWeaponSkinModelsMethod),
            SkinChangePopupLocalPatch.TargetCreateWeaponSkinModelsMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixCreateWeaponSkinModels)),
                postfix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PostfixCreateWeaponSkinModels)),
                finalizer: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.FinalizeCreateWeaponSkinModels)))),
        new PatchRegistration(
            nameof(SkinChangePopupLocalPatch) + "." + nameof(SkinChangePopupLocalPatch.TargetConfirmSubscriptionMethod),
            SkinChangePopupLocalPatch.TargetConfirmSubscriptionMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixConfirmSubscription)))),
        new PatchRegistration(
            nameof(SkinChangePopupLocalPatch) + "." + nameof(SkinChangePopupLocalPatch.TargetConfirmMethod),
            SkinChangePopupLocalPatch.TargetConfirmMethod,
            static (harmony, target) => harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(SkinChangePopupLocalPatch), nameof(SkinChangePopupLocalPatch.PrefixConfirm)))),
    };

    internal static PreflightResult PreflightTargets() => CheckTargetResolvers(GetRegistrations());

    internal static PreflightResult CheckTargetResolvers(IEnumerable<PatchRegistration> registrations)
    {
        var failures = new List<string>();
        var installers = new List<Action<Harmony>>();
        var targetCount = 0;
        foreach (var registration in registrations)
        {
            targetCount++;
            try
            {
                var target = registration.Resolve();
                if (target == null)
                {
                    failures.Add(registration.Name + " => missing-target");
                    continue;
                }

                installers.Add(harmony => registration.Install(harmony, target));
            }
            catch (Exception exception)
            {
                failures.Add(registration.Name + " => precheck-exception:" + exception.GetType().Name + ":" + exception.Message);
            }
        }

        return new PreflightResult(targetCount, failures, installers);
    }

    internal static void ApplyInstallersWithRollback(
        IReadOnlyList<Action<Harmony>> installers,
        Harmony harmony,
        Action rollback)
    {
        try
        {
            foreach (var install in installers)
            {
                install(harmony);
            }
        }
        catch (Exception installException)
        {
            try
            {
                rollback();
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "Harmony installation and rollback both failed.",
                    installException,
                    rollbackException);
            }

            throw;
        }
    }

    internal static void Initialize(PreflightResult preflight)
    {
        if (preflight.Failures.Count > 0)
        {
            throw new InvalidOperationException("Cannot install CGUnlock patches after a failed compatibility precheck.");
        }

        var harmony = new Harmony(PluginInfo.PluginGuid);
        ApplyInstallersWithRollback(preflight.Installers, harmony, harmony.UnpatchSelf);
        CgUnlockPlugin.LogSource.LogInfo("[CGUnlock] Harmony patch registration complete.");
    }
}
