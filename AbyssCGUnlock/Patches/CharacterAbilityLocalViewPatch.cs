#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Absf;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Project;
using Project.CharacterDetail;
using Project.Common;
using Project.Master;
using Project.User;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Builds the skill/ability portion of a dynamically discovered unowned character from downloaded
/// Master data. The native user-data factory expects account-owned ability rows and therefore
/// produces an empty skill tab for registry-backed characters. All mutation controls are disabled
/// after the native view renders, leaving icons and descriptions available as a read-only preview.
/// </summary>
internal static class CharacterAbilityLocalViewPatch
{
    private static readonly HashSet<IntPtr> DisabledButtonPointers = new();
    private static readonly HashSet<IntPtr> BlockedSkillModelPointers = new();
    private static readonly HashSet<IntPtr> OwnedSkillModelPointers = new();
    private static readonly HashSet<IntPtr> BlockedSkillViewPointers = new();
    private static readonly HashSet<IntPtr> OwnedSkillViewPointers = new();
    private static bool _loggedFirstMasterModel;
    private static bool _loggedFirstReadOnlyView;
    private static bool _loggedFirstBlockedMutation;

    internal static MethodBase TargetModelFactoryMethod()
    {
        return AccessTools.Method(
            typeof(CharacterDetailModel),
            nameof(CharacterDetailModel.CreateFromUserData),
            new[] { typeof(MasterDataStore), typeof(CharacterData) });
    }

    internal static MethodBase TargetReadOnlyViewMethod()
    {
        return AccessTools.Method(
            typeof(CharacterAbilityUpInfoBoxViewService),
            nameof(CharacterAbilityUpInfoBoxViewService.UpdateView),
            new[] { typeof(CharacterDetailModel) });
    }

    internal static MethodBase TargetAbilityUpCommandMethod()
    {
        return AccessTools.Method(
            typeof(CharacterAbilityUpInfoBoxViewService.__c),
            nameof(CharacterAbilityUpInfoBoxViewService.__c._InitializeViewAsync_b__15_1),
            new[] { typeof(UniRx.Unit), typeof(CharacterAbilityUpInfoBoxViewService) });
    }

    internal static MethodBase TargetUnlockCommandMethod()
    {
        return AccessTools.Method(
            typeof(CharacterAbilityUpInfoBoxViewService.__c__DisplayClass33_0),
            nameof(CharacterAbilityUpInfoBoxViewService.__c__DisplayClass33_0._OpenUnlockConfirmPopupAsync_b__0),
            Type.EmptyTypes);
    }

    internal static bool PrefixModelFactory(
        MasterDataStore dataStore,
        CharacterData character,
        ref CharacterDetailModel? __result,
        out bool __state)
    {
        // With the feature enabled, unknown provenance is conservatively read-only. We clear this
        // only after positively matching the exact CharacterData object in the account-owned list.
        __state = PluginConfig.EnableUnownedCharacterSkillView.Value && character != null;

        if (!PluginConfig.EnableUnownedCharacterSkillView.Value ||
            dataStore == null ||
            character == null)
        {
            return true;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            var isRegistryCharacter = userData != null &&
                LocalCharacterRegistry.TryGet(userData, character.MCharaId, out var registered) &&
                registered != null &&
                IL2CPP.Il2CppObjectBaseToPtr(registered) == IL2CPP.Il2CppObjectBaseToPtr(character);

            // Registry identity wins over _dataList membership: CharacterDetailLocalUnlockPatch
            // temporarily injects this exact synthetic object while native detail rendering runs.
            if (!isRegistryCharacter)
            {
                if (IsExactOwnedCharacterRow(userData, character))
                {
                    __state = false;
                }

                return true;
            }

            var level = character.ParameterData == null
                ? 1
                : Math.Max(1, character.ParameterData.Lv);

            __result = CharacterDetailModel.CreateFromMaster(
                dataStore,
                character.MCharaId,
                level,
                character.LimitBreakCount,
                false);

            if (__result == null)
            {
                CgUnlockPlugin.LogSource.LogWarning(
                    $"[CGUnlock] 未持有角色技能Master模型创建返回空，回退账号模型: id={character.MCharaId}");
                return true;
            }

            if (!_loggedFirstMasterModel)
            {
                _loggedFirstMasterModel = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 未持有角色技能已切换到Master只读模型: first_id={character.MCharaId}, level={level}, limit_break={character.LimitBreakCount}");
            }

            return false;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色技能Master建模失败，回退账号模型: id={character.MCharaId}, error={exception}");
            return true;
        }
    }

    internal static void PostfixModelFactory(
        CharacterDetailModel? __result,
        bool __state)
    {
        if (__result == null)
        {
            return;
        }

        try
        {
            var modelPointer = IL2CPP.Il2CppObjectBaseToPtr(__result);
            if (__state)
            {
                OwnedSkillModelPointers.Remove(modelPointer);
                BlockedSkillModelPointers.Add(modelPointer);
                return;
            }

            BlockedSkillModelPointers.Remove(modelPointer);
            OwnedSkillModelPointers.Add(modelPointer);
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 技能模型来源标记失败；后续未知视图将保持只读: error={exception}");
        }
    }

    internal static void PrefixRestoreTrackedButtons(
        CharacterAbilityUpInfoBoxViewService __instance)
    {
        if (__instance == null)
        {
            return;
        }

        try
        {
            // Invalidate the previous character's ownership before native UpdateView. Only a
            // successful postfix may promote this reused service back to account-owned.
            var viewPointer = IL2CPP.Il2CppObjectBaseToPtr(__instance);
            OwnedSkillViewPointers.Remove(viewPointer);
            BlockedSkillViewPointers.Add(viewPointer);
        }
        catch (Exception exception)
        {
            // If this particular native pointer cannot be read, discard every positive grant. A
            // later successful postfix will rebuild only the currently valid owned-view grant.
            OwnedSkillViewPointers.Clear();
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 技能视图切换前来源标记失效失败；命令边界仍按未知来源阻断: error={exception}");
        }

        // Undo only this patch's previous component.enabled changes before native UpdateView runs.
        // Native code can then calculate the selected owned character's button state from scratch.
        ApplyAllMutationButtonStatesBestEffort(__instance, false, 0);
    }

    internal static void PostfixReadOnlyView(
        CharacterAbilityUpInfoBoxViewService __instance,
        CharacterDetailModel characterDetailModel)
    {
        if (__instance == null || characterDetailModel == null)
        {
            return;
        }

        bool readOnly;
        IntPtr viewPointer;
        try
        {
            viewPointer = IL2CPP.Il2CppObjectBaseToPtr(__instance);
            var modelPointer = IL2CPP.Il2CppObjectBaseToPtr(characterDetailModel);
            if (BlockedSkillModelPointers.Contains(modelPointer))
            {
                readOnly = true;
            }
            else if (OwnedSkillModelPointers.Contains(modelPointer))
            {
                readOnly = false;
            }
            else
            {
                // Models created outside the patched factory are unknown until the current account
                // positively contains the same character ID. Unknown/unowned IDs stay read-only.
                readOnly = !IsCharacterIdOwnedByCurrentAccount(characterDetailModel.MCharacterId);
            }
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 技能视图来源识别失败，按未知来源保持只读: id={characterDetailModel.MCharacterId}, error={exception}");
            try
            {
                viewPointer = IL2CPP.Il2CppObjectBaseToPtr(__instance);
            }
            catch
            {
                // A broken view cannot be interacted with safely; no marker can be attached.
                return;
            }

            readOnly = true;
        }

        if (readOnly)
        {
            OwnedSkillViewPointers.Remove(viewPointer);
            BlockedSkillViewPointers.Add(viewPointer);
        }
        else
        {
            BlockedSkillViewPointers.Remove(viewPointer);
            OwnedSkillViewPointers.Add(viewPointer);
        }

        ApplyAllMutationButtonStatesBestEffort(
            __instance,
            readOnly,
            characterDetailModel.MCharacterId);

        if (readOnly && !_loggedFirstReadOnlyView)
        {
            _loggedFirstReadOnlyView = true;
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 未持有角色技能升级入口已本地禁用(只读展示): first_id={characterDetailModel.MCharacterId}");
        }
    }

    internal static bool PrefixAbilityUpCommand(CharacterAbilityUpInfoBoxViewService viewService)
    {
        return !ShouldBlockMutationCommand(viewService, "ability_up");
    }

    internal static bool PrefixUnlockCommand(
        CharacterAbilityUpInfoBoxViewService.__c__DisplayClass33_0 __instance)
    {
        CharacterAbilityUpInfoBoxViewService? viewService = null;
        try
        {
            viewService = __instance?.__4__this;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色技能解锁命令来源解析失败，按未知来源阻断: error={exception}");
            return false;
        }

        return !ShouldBlockMutationCommand(viewService, "ability_unlock");
    }

    private static bool IsCharacterIdOwnedByCurrentAccount(long mCharacterId)
    {
        if (!PluginConfig.EnableUnownedCharacterSkillView.Value)
        {
            return true;
        }

        var userData = Engine.Get<UserData>();
        if (userData == null)
        {
            return false;
        }

        var ownedRows = userData._CharaDataStore_k__BackingField?._dataList;
        if (ownedRows == null)
        {
            return false;
        }

        var registryPointer = IntPtr.Zero;
        if (LocalCharacterRegistry.TryGet(userData, mCharacterId, out var registered) &&
            registered != null)
        {
            registryPointer = IL2CPP.Il2CppObjectBaseToPtr(registered);
        }

        for (var i = 0; i < ownedRows.Count; i++)
        {
            var row = ownedRows[i];
            if (row != null &&
                row.MCharaId == mCharacterId &&
                (registryPointer == IntPtr.Zero ||
                 IL2CPP.Il2CppObjectBaseToPtr(row) != registryPointer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExactOwnedCharacterRow(UserData? userData, CharacterData character)
    {
        var ownedRows = userData?._CharaDataStore_k__BackingField?._dataList;
        if (ownedRows == null)
        {
            return false;
        }

        var characterPointer = IL2CPP.Il2CppObjectBaseToPtr(character);
        for (var i = 0; i < ownedRows.Count; i++)
        {
            var row = ownedRows[i];
            if (row != null &&
                row.MCharaId == character.MCharaId &&
                IL2CPP.Il2CppObjectBaseToPtr(row) == characterPointer)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldBlockMutationCommand(
        CharacterAbilityUpInfoBoxViewService? viewService,
        string command)
    {
        if (viewService == null)
        {
            return PluginConfig.EnableUnownedCharacterSkillView.Value;
        }

        if (!PluginConfig.EnableUnownedCharacterSkillView.Value)
        {
            return false;
        }

        try
        {
            var viewPointer = IL2CPP.Il2CppObjectBaseToPtr(viewService);
            if (OwnedSkillViewPointers.Contains(viewPointer))
            {
                return false;
            }

            if (!_loggedFirstBlockedMutation && BlockedSkillViewPointers.Contains(viewPointer))
            {
                _loggedFirstBlockedMutation = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 未持有角色技能变更命令已在同步点击边界拦截: command={command}");
            }

            // Only a view positively tagged from an exact account-owned CharacterData row is
            // allowed through. Tagged-local and unknown views both fail closed before UniTask/API.
            return true;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色技能变更命令识别失败，按未知来源阻断: command={command}, error={exception}");
            return true;
        }
    }

    private static void ApplyAllMutationButtonStatesBestEffort(
        CharacterAbilityUpInfoBoxViewService viewService,
        bool readOnly,
        long mCharacterId)
    {
        ApplyButtonStateBestEffort(
            () => viewService._abilityUpButton,
            readOnly,
            mCharacterId,
            "ability_up");
        ApplyButtonStateBestEffort(
            () => viewService._resetButton,
            readOnly,
            mCharacterId,
            "reset");

        Il2CppSystem.Collections.Generic.List<CharacterAbilityUpInformationView>? informationViews;
        try
        {
            informationViews = viewService._abilityUpInformationViewList;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色技能行列表读取失败，主变更命令仍保持拦截: id={mCharacterId}, error={exception}");
            return;
        }

        if (informationViews == null)
        {
            return;
        }

        for (var i = 0; i < informationViews.Count; i++)
        {
            var informationView = informationViews[i];
            if (informationView == null)
            {
                continue;
            }

            ApplyButtonStateBestEffort(
                () => informationView._unlockButton,
                readOnly,
                mCharacterId,
                $"unlock[{i}]");

            Project.Outgame.FluctuationButtonGroup? fluctuationButtonGroup;
            try
            {
                fluctuationButtonGroup = informationView._fluctuationButtonGroup;
            }
            catch (Exception exception)
            {
                CgUnlockPlugin.LogSource.LogWarning(
                    $"[CGUnlock] 未持有角色技能等级按钮组读取失败: id={mCharacterId}, index={i}, error={exception}");
                continue;
            }

            if (fluctuationButtonGroup == null)
            {
                continue;
            }

            ApplyButtonStateBestEffort(() => fluctuationButtonGroup._plusButton, readOnly, mCharacterId, $"plus[{i}]");
            ApplyButtonStateBestEffort(() => fluctuationButtonGroup._minusButton, readOnly, mCharacterId, $"minus[{i}]");
            ApplyButtonStateBestEffort(() => fluctuationButtonGroup._maxButton, readOnly, mCharacterId, $"max[{i}]");
            ApplyButtonStateBestEffort(() => fluctuationButtonGroup._resetButton, readOnly, mCharacterId, $"row_reset[{i}]");
        }
    }

    private static void ApplyButtonStateBestEffort(
        Func<AppButton?> resolveButton,
        bool readOnly,
        long mCharacterId,
        string control)
    {
        try
        {
            SetReadOnly(resolveButton(), readOnly);
        }
        catch (Exception exception)
        {
            // Continue with every remaining control; the synchronous command prefixes are the
            // defense-in-depth boundary if a Unity object disappears during this UI pass.
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色技能只读按钮处理失败: id={mCharacterId}, control={control}, error={exception}");
        }
    }

    private static void SetReadOnly(AppButton? button, bool readOnly)
    {
        if (button == null)
        {
            return;
        }

        if (readOnly)
        {
            var pointer = IL2CPP.Il2CppObjectBaseToPtr(button);
            if (button.enabled)
            {
                button.enabled = false;
                DisabledButtonPointers.Add(pointer);
            }

            button.interactable = false;
            return;
        }

        // This restoration normally runs in PrefixRestoreTrackedButtons, before native UpdateView
        // recalculates owned-character state. The postfix call is then a no-op for owned views.
        var ownedViewPointer = IL2CPP.Il2CppObjectBaseToPtr(button);
        if (DisabledButtonPointers.Remove(ownedViewPointer))
        {
            button.enabled = true;
        }
    }
}
