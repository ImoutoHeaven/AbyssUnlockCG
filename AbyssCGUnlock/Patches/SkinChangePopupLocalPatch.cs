#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Project.CharacterDetail;
using Project.Master;
using Project.Master.NoaMessagePack;
using Project.Outgame.UI;
using Project.Outgame.UI.Popup;
using Project.User;
using UniRx;
using Il2CppCharacterDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterData>;
using Il2CppCharacterSkinDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterSkinsData>;
using Il2CppMasterCharacterSkinList = Il2CppSystem.Collections.Generic.List<Project.Master.NoaMessagePack.MCharacterSkins>;
using Il2CppSkinCellList = Il2CppSystem.Collections.Generic.List<Project.CharacterDetail.CharacterSkinCellModel>;
using Il2CppWeaponSkinCellList = Il2CppSystem.Collections.Generic.List<Project.CharacterDetail.WeaponSkinCellModel>;
using Il2CppWeaponSkinDataList = Il2CppSystem.Collections.Generic.List<Project.User.WeaponSkinsData>;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Exposes downloaded, released character skins through the native popup and commits the selected
/// portrait/SD pair to process-local state. The native API-backed confirmation state machine is
/// suppressed whenever a character skin changes or the character is synthetic.
/// </summary>
internal static class SkinChangePopupLocalPatch
{
    private const int BattleCharacterSkinType = 1;
    private const int TavernCharacterSkinType = 2;
    private const int DefaultCharacterSkinFlag = 1;

    internal sealed class CharacterInjectionState
    {
        internal CharacterInjectionState(Il2CppCharacterDataList list, CharacterData character)
        {
            List = list;
            Character = character;
        }

        internal Il2CppCharacterDataList List { get; }
        internal CharacterData Character { get; }
    }

    internal sealed class EntitlementInjectionState
    {
        internal EntitlementInjectionState(Il2CppCharacterSkinDataList list)
        {
            List = list;
            Added = new List<CharacterSkinsData>();
        }

        internal Il2CppCharacterSkinDataList List { get; }
        internal List<CharacterSkinsData> Added { get; }
    }

    internal sealed class WeaponEntitlementInjectionState
    {
        internal WeaponEntitlementInjectionState(
            Il2CppWeaponSkinDataList list,
            WeaponSkinsData added)
        {
            List = list;
            Added = added;
        }

        internal Il2CppWeaponSkinDataList List { get; }
        internal WeaponSkinsData Added { get; }
    }

    private static bool _loggedFirstEntitlementExpansion;
    private static bool _loggedFirstSessionRestore;
    private static bool _loggedFirstCharacterFallback;
    private static bool _loggedFirstWeaponFallback;

    internal static MethodBase TargetInitializePopupMethod()
    {
        return AccessTools.Method(
            typeof(SkinChangePopupController),
            nameof(SkinChangePopupController.InitializePopup),
            new[] { typeof(SkinChangePopup) });
    }

    internal static MethodBase TargetCreateCharacterSkinModelsMethod()
    {
        return AccessTools.Method(
            typeof(SkinChangePopupController),
            nameof(SkinChangePopupController.CreateCharacterSkinCellModels),
            new[] { typeof(Il2CppMasterCharacterSkinList), typeof(long) });
    }

    internal static MethodBase TargetCreateWeaponSkinModelsMethod()
    {
        return AccessTools.Method(
            typeof(SkinChangePopupController),
            nameof(SkinChangePopupController.CreateWeaponSkinCellModels),
            new[] { typeof(MCharacters), typeof(long) });
    }

    internal static MethodBase TargetConfirmMethod()
    {
        return AccessTools.Method(
            typeof(SkinChangePopupController),
            nameof(SkinChangePopupController.OnClickConfirm),
            Type.EmptyTypes);
    }

    internal static MethodBase TargetConfirmSubscriptionMethod()
    {
        return AccessTools.Method(
            typeof(SkinChangePopupController.__c),
            nameof(SkinChangePopupController.__c._SetupPopupEvent_b__13_1),
            new[] { typeof(Unit), typeof(SkinChangePopupController) });
    }

    internal static void PrefixInitializePopup(
        SkinChangePopupController __instance,
        out CharacterInjectionState? __state)
    {
        __state = null;
        if (!PluginConfig.EnableLocalCharacterSkinChange.Value || __instance == null)
        {
            return;
        }

        try
        {
            var userData = __instance._userData;
            var characterId = __instance._mCharacterId;
            if (LocalCharacterSkinRegistry.ApplySavedSelection(
                    userData,
                    characterId,
                    __instance._masterDataStore) &&
                !_loggedFirstSessionRestore)
            {
                _loggedFirstSessionRestore = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 本地换装会话状态已恢复到角色详情: first_character_id={characterId}");
            }

            if (userData == null ||
                !LocalCharacterRegistry.TryGet(userData, characterId, out var synthetic) ||
                synthetic == null)
            {
                return;
            }

            var characterRows = userData._CharaDataStore_k__BackingField?._dataList;
            if (characterRows == null || ContainsCharacter(characterRows, characterId))
            {
                return;
            }

            synthetic._IsNew_k__BackingField = false;
            synthetic._IsExistStory_k__BackingField = true;
            characterRows.Add(synthetic);
            __state = new CharacterInjectionState(characterRows, synthetic);
        }
        catch (Exception exception)
        {
            RemoveCharacterInjection(__state);
            __state = null;
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 换装弹窗本地角色准备失败，保留纯本地拦截: error={exception}");
        }
    }

    internal static Exception? FinalizeInitializePopup(
        Exception? __exception,
        CharacterInjectionState? __state)
    {
        RemoveCharacterInjection(__state);
        return __exception;
    }

    internal static void PrefixCreateCharacterSkinModels(
        SkinChangePopupController __instance,
        Il2CppMasterCharacterSkinList mCharacterSkins,
        ref long currentMSkinId,
        out EntitlementInjectionState? __state)
    {
        __state = null;
        if (!PluginConfig.EnableLocalCharacterSkinChange.Value ||
            __instance == null ||
            mCharacterSkins == null)
        {
            return;
        }

        try
        {
            var userData = __instance._userData;
            var entitlementRows = userData?._CharacterSkinsDataStore_k__BackingField?.DataList;
            if (entitlementRows == null)
            {
                return;
            }

            var candidates = new List<long>(mCharacterSkins.Count);
            var selectionCandidates =
                new List<(long SkinId, bool IsDefault, int DisplayOrder)>(mCharacterSkins.Count);
            for (var i = 0; i < mCharacterSkins.Count; i++)
            {
                var masterSkin = mCharacterSkins[i];
                if (masterSkin != null)
                {
                    candidates.Add(masterSkin.id);
                    selectionCandidates.Add((
                        masterSkin.id,
                        masterSkin.is_default == DefaultCharacterSkinFlag,
                        masterSkin.display_order));
                }
            }

            if (LocalCharacterSkinRegistry.IsSyntheticCharacter(
                    userData,
                    __instance._mCharacterId))
            {
                var resolvedCurrentSkinId = CharacterSkinModelSelectionPolicy.ResolveCurrentSkinId(
                    currentMSkinId,
                    selectionCandidates);
                var resolvedCurrentSkin = FindCharacterSkinMaster(
                    mCharacterSkins,
                    resolvedCurrentSkinId);
                if (resolvedCurrentSkin != null)
                {
                    currentMSkinId = resolvedCurrentSkin.id;
                    ApplyCurrentCharacterSkin(__instance, resolvedCurrentSkin);
                }
            }

            var owned = new List<long>(entitlementRows.Count);
            for (var i = 0; i < entitlementRows.Count; i++)
            {
                var row = entitlementRows[i];
                if (row != null)
                {
                    owned.Add(row._MCharacterSkinId_k__BackingField);
                }
            }

            var planned = DynamicCharacterSkinEntitlementPlanner.Plan(candidates, owned);
            LocalCharacterSkinRegistry.RegisterLocalEntitlements(
                userData,
                __instance._mCharacterId,
                planned);
            if (planned.Count == 0)
            {
                return;
            }

            var state = new EntitlementInjectionState(entitlementRows);
            __state = state;
            var userId = GetUserId(userData);
            for (var i = 0; i < planned.Count; i++)
            {
                var row = new CharacterSkinsData
                {
                    _Id_k__BackingField = 0,
                    _UserId_k__BackingField = userId,
                    _MCharacterSkinId_k__BackingField = planned[i]
                };

                entitlementRows.Add(row);
                state.Added.Add(row);
            }

            if (!_loggedFirstEntitlementExpansion)
            {
                _loggedFirstEntitlementExpansion = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 换装弹窗已动态注册下载缓存皮肤: first_character_id={__instance._mCharacterId}, " +
                    $"candidates={candidates.Count}, locally_unlocked={state.Added.Count}");
            }
        }
        catch (Exception exception)
        {
            RemoveEntitlementInjection(__state);
            __state = null;
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 下载缓存皮肤动态注册失败，回退原生皮肤列表: character_id={__instance._mCharacterId}, error={exception}");
        }
    }

    internal static void PostfixCreateCharacterSkinModels(
        SkinChangePopupController __instance,
        Il2CppMasterCharacterSkinList mCharacterSkins,
        long currentMSkinId,
        ref Il2CppSkinCellList __result)
    {
        if (!PluginConfig.EnableLocalCharacterSkinChange.Value ||
            __instance == null ||
            mCharacterSkins == null ||
            !LocalCharacterSkinRegistry.IsSyntheticCharacter(
                __instance._userData,
                __instance._mCharacterId) ||
            HasSelectedCharacterSkin(__result))
        {
            return;
        }

        try
        {
            var selectionCandidates =
                new List<(long SkinId, bool IsDefault, int DisplayOrder)>(mCharacterSkins.Count);
            for (var i = 0; i < mCharacterSkins.Count; i++)
            {
                var candidate = mCharacterSkins[i];
                if (candidate != null)
                {
                    selectionCandidates.Add((
                        candidate.id,
                        candidate.is_default == DefaultCharacterSkinFlag,
                        candidate.display_order));
                }
            }

            var resolvedCurrentSkinId = CharacterSkinModelSelectionPolicy.ResolveCurrentSkinId(
                currentMSkinId,
                selectionCandidates);
            var masterSkin = FindCharacterSkinMaster(mCharacterSkins, resolvedCurrentSkinId);
            if (masterSkin == null)
            {
                return;
            }

            __result ??= new Il2CppSkinCellList();
            UnlockMatchingCharacterSkin(__result, masterSkin.id);
            if (!HasSelectedCharacterSkin(__result))
            {
                __result.Add(CreateCharacterSkinCellModel(__instance, masterSkin));
            }

            ApplyCurrentCharacterSkin(__instance, masterSkin);
            if (!_loggedFirstCharacterFallback)
            {
                _loggedFirstCharacterFallback = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 未持有角色换装弹窗已补齐当前角色皮肤模型: " +
                    $"first_character_id={__instance._mCharacterId}, character_skin_id={masterSkin.id}");
            }
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色当前皮肤模型Master回退失败: " +
                $"character_id={__instance._mCharacterId}, error={exception}");
        }
    }

    internal static Exception? FinalizeCreateCharacterSkinModels(
        Exception? __exception,
        EntitlementInjectionState? __state)
    {
        RemoveEntitlementInjection(__state);
        return __exception;
    }

    internal static void PrefixCreateWeaponSkinModels(
        SkinChangePopupController __instance,
        MCharacters mCharacter,
        ref long currentMSkinId,
        out WeaponEntitlementInjectionState? __state)
    {
        __state = null;
        if (!PluginConfig.EnableLocalCharacterSkinChange.Value ||
            __instance == null ||
            mCharacter == null ||
            !LocalCharacterSkinRegistry.IsSyntheticCharacter(
                __instance._userData,
                __instance._mCharacterId))
        {
            return;
        }

        try
        {
            var masterWeaponSkin = FindCompatibleWeaponSkin(
                __instance,
                mCharacter,
                currentMSkinId);
            if (masterWeaponSkin == null)
            {
                return;
            }

            currentMSkinId = masterWeaponSkin.id;
            __instance._currentMWeaponSkinId = masterWeaponSkin.id;

            var entitlementRows = __instance._userData?
                ._WeaponSkinsDataStore_k__BackingField?
                .DataList;
            if (entitlementRows == null ||
                ContainsWeaponSkinEntitlement(entitlementRows, masterWeaponSkin.id))
            {
                return;
            }

            var row = new WeaponSkinsData
            {
                _Id_k__BackingField = 0,
                _UserId_k__BackingField = GetUserId(__instance._userData),
                _MWeaponSkinId_k__BackingField = masterWeaponSkin.id
            };
            entitlementRows.Add(row);
            __state = new WeaponEntitlementInjectionState(entitlementRows, row);
        }
        catch (Exception exception)
        {
            RemoveWeaponEntitlementInjection(__state);
            __state = null;
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色武器皮肤准备失败，将尝试Master回退: " +
                $"character_id={__instance._mCharacterId}, error={exception}");
        }
    }

    internal static void PostfixCreateWeaponSkinModels(
        SkinChangePopupController __instance,
        MCharacters mCharacter,
        long currentMSkinId,
        ref Il2CppWeaponSkinCellList __result)
    {
        if (!PluginConfig.EnableLocalCharacterSkinChange.Value ||
            __instance == null ||
            mCharacter == null ||
            !LocalCharacterSkinRegistry.IsSyntheticCharacter(
                __instance._userData,
                __instance._mCharacterId) ||
            HasSelectedWeaponSkin(__result))
        {
            return;
        }

        try
        {
            var masterWeaponSkin = FindCompatibleWeaponSkin(
                __instance,
                mCharacter,
                currentMSkinId > 0 ? currentMSkinId : __instance._currentMWeaponSkinId);
            if (masterWeaponSkin == null)
            {
                return;
            }

            __result ??= new Il2CppWeaponSkinCellList();
            UnlockMatchingWeaponSkin(__result, masterWeaponSkin.id);
            if (!HasSelectedWeaponSkin(__result) &&
                SkinChangePopupCompletenessPolicy.NeedsSyntheticWeaponFallback(true, __result.Count))
            {
                __result.Add(CreateWeaponSkinCellModel(__instance, masterWeaponSkin));
            }

            if (!HasSelectedWeaponSkin(__result))
            {
                __result.Add(CreateWeaponSkinCellModel(__instance, masterWeaponSkin));
            }

            __instance._currentMWeaponSkinId = masterWeaponSkin.id;
            if (!_loggedFirstWeaponFallback)
            {
                _loggedFirstWeaponFallback = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 未持有角色换装弹窗已补齐本地武器模型: " +
                    $"first_character_id={__instance._mCharacterId}, weapon_skin_id={masterWeaponSkin.id}");
            }
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色武器模型Master回退失败: " +
                $"character_id={__instance._mCharacterId}, error={exception}");
        }
    }

    internal static Exception? FinalizeCreateWeaponSkinModels(
        Exception? __exception,
        WeaponEntitlementInjectionState? __state)
    {
        RemoveWeaponEntitlementInjection(__state);
        return __exception;
    }

    internal static bool PrefixConfirm(SkinChangePopupController __instance)
    {
        if (!PluginConfig.EnableLocalCharacterSkinChange.Value || __instance == null)
        {
            return true;
        }

        var characterId = 0L;
        try
        {
            characterId = __instance._mCharacterId;
            var userData = __instance._userData;
            var model = __instance._model;
            var current = new CharacterSkinSelection(
                __instance._currentBattleMCharacterSkinId,
                __instance._currentTavernMCharacterSkinId);

            var availableBattle = CollectAvailableSkinIds(model?.NormalCharacterSkinCellModels);
            var availableTavern = CollectAvailableSkinIds(model?.TavernCharacterSkinCellModels);
            var selected = new CharacterSkinSelection(
                FindSelectedSkinId(model?.NormalCharacterSkinCellModels, current.BattleSkinId),
                FindSelectedSkinId(model?.TavernCharacterSkinCellModels, current.TavernSkinId));
            var resolved = CharacterSkinSelectionPolicy.Resolve(
                current,
                selected,
                availableBattle,
                availableTavern);
            var isSynthetic = LocalCharacterSkinRegistry.IsSyntheticCharacter(userData, characterId);
            var isRegisteredLocalSkin = LocalCharacterSkinRegistry.IsLocallyUnlocked(
                userData,
                characterId,
                resolved);
            var requiresLocalEntitlement = isRegisteredLocalSkin ||
                                           RequiresLocalCharacterSkinEntitlement(
                                               __instance,
                                               userData,
                                               resolved);

            if (!CharacterSkinSelectionPolicy.ShouldHandleLocally(
                    isSynthetic,
                    current,
                    resolved,
                    requiresLocalEntitlement))
            {
                return true;
            }

            if (!LocalCharacterSkinRegistry.Save(userData, characterId, resolved) ||
                !LocalCharacterSkinRegistry.ApplySelection(
                    userData,
                    characterId,
                    resolved,
                    __instance._masterDataStore))
            {
                CgUnlockPlugin.LogSource.LogWarning(
                    $"[CGUnlock] 本地换装状态无法绑定当前角色，已阻断服务端请求: character_id={characterId}");
                return false;
            }

            __instance._currentBattleMCharacterSkinId = resolved.BattleSkinId;
            __instance._currentTavernMCharacterSkinId = resolved.TavernSkinId;
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 角色换装已本地确认并缓存: character_id={characterId}, " +
                $"battle_skin_id={resolved.BattleSkinId}, tavern_skin_id={resolved.TavernSkinId}, " +
                $"synthetic={isSynthetic}, local_entitlement={requiresLocalEntitlement}, " +
                "server_request=false");

            try
            {
                __instance._onChanged?.Invoke();
            }
            finally
            {
                __instance.ClosePopup();
            }

            return false;
        }
        catch (Exception exception)
        {
            // Fail closed after entering the local-skin path: the original method immediately starts
            // an async state machine that sends character/weapon skin requests.
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 本地换装确认失败，已阻断服务端请求: character_id={characterId}, error={exception}");
            return false;
        }
    }

    internal static bool PrefixConfirmSubscription(SkinChangePopupController __1)
    {
        return PrefixConfirm(__1);
    }

    private static bool ContainsCharacter(Il2CppCharacterDataList rows, long characterId)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row != null &&
                (row.MCharaId == characterId || row._Id_k__BackingField == characterId))
            {
                return true;
            }
        }

        return false;
    }

    private static long GetUserId(UserData? userData)
    {
        try
        {
            return userData?._UserStatus_k__BackingField?._UserId_k__BackingField ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static MCharacterSkins? FindCharacterSkinMaster(
        Il2CppMasterCharacterSkinList skins,
        long skinId)
    {
        if (skinId <= 0)
        {
            return null;
        }

        for (var i = 0; i < skins.Count; i++)
        {
            var skin = skins[i];
            if (skin != null && skin.id == skinId)
            {
                return skin;
            }
        }

        return null;
    }

    private static void ApplyCurrentCharacterSkin(
        SkinChangePopupController controller,
        MCharacterSkins masterSkin)
    {
        CharacterData? synthetic = null;
        if (controller._userData != null)
        {
            LocalCharacterRegistry.TryGet(
                controller._userData,
                controller._mCharacterId,
                out synthetic);
        }

        if (masterSkin.type == BattleCharacterSkinType)
        {
            controller._currentBattleMCharacterSkinId = masterSkin.id;
            if (synthetic != null)
            {
                synthetic._BattleMCharacterSkinId_k__BackingField = masterSkin.id;
            }
        }
        else if (masterSkin.type == TavernCharacterSkinType)
        {
            controller._currentTavernMCharacterSkinId = masterSkin.id;
            if (synthetic != null)
            {
                synthetic._TavernMCharacterSkinId_k__BackingField = masterSkin.id;
            }
        }
    }

    private static bool HasSelectedCharacterSkin(Il2CppSkinCellList? cells)
    {
        if (cells == null)
        {
            return false;
        }

        for (var i = 0; i < cells.Count; i++)
        {
            if (cells[i]?.IsSelected == true)
            {
                return true;
            }
        }

        return false;
    }

    private static void UnlockMatchingCharacterSkin(
        Il2CppSkinCellList cells,
        long skinId)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell?.ThumbnailModel?.ContentId != skinId)
            {
                continue;
            }

            cell.IsLocked = false;
            cell.IsEquipped = true;
            cell.IsSelected = true;
            return;
        }
    }

    private static CharacterSkinCellModel CreateCharacterSkinCellModel(
        SkinChangePopupController controller,
        MCharacterSkins master)
    {
        return new CharacterSkinCellModel
        {
            Name = master.name ?? string.Empty,
            Description = master.description ?? string.Empty,
            AssetId = master.asset_id ?? string.Empty,
            IsDefault = master.is_default,
            IsLocked = false,
            IsEquipped = true,
            IsSelected = true,
            ThumbnailModel = CharacterSkinThumbnailModel.CreateWithTypeIcon(
                master.id,
                controller._cancellationToken),
            DisplayOrder = master.display_order,
            ReleasedAt = Project.DateTimeUtility.ConvertToDateTimeOrDefault(master.released_at)
        };
    }

    private static bool RequiresLocalCharacterSkinEntitlement(
        SkinChangePopupController controller,
        UserData? userData,
        CharacterSkinSelection selection)
    {
        var rows = userData?._CharacterSkinsDataStore_k__BackingField?.DataList;
        return RequiresLocalCharacterSkinEntitlement(controller, rows, selection.BattleSkinId) ||
               RequiresLocalCharacterSkinEntitlement(controller, rows, selection.TavernSkinId);
    }

    private static bool RequiresLocalCharacterSkinEntitlement(
        SkinChangePopupController controller,
        Il2CppCharacterSkinDataList? rows,
        long skinId)
    {
        if (skinId <= 0)
        {
            return false;
        }

        if (rows != null)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row != null && row._MCharacterSkinId_k__BackingField == skinId)
                {
                    return false;
                }
            }
        }

        var masterSkins = controller._masterDataStore?.GetCache<MCharacterSkins>();
        if (masterSkins != null)
        {
            for (var i = 0; i < masterSkins.Length; i++)
            {
                var masterSkin = masterSkins[i];
                if (masterSkin != null && masterSkin.id == skinId)
                {
                    return CharacterSkinEntitlementPolicy.RequiresLocalEntitlement(
                        false,
                        true,
                        masterSkin.is_default == DefaultCharacterSkinFlag);
                }
            }
        }

        return CharacterSkinEntitlementPolicy.RequiresLocalEntitlement(false, false, false);
    }

    private static MWeaponSkins? FindCompatibleWeaponSkin(
        SkinChangePopupController controller,
        MCharacters character,
        long preferredSkinId)
    {
        var masterWeaponSkins = controller._masterDataStore?.GetCache<MWeaponSkins>();
        if (masterWeaponSkins == null)
        {
            return null;
        }

        MWeaponSkins? best = null;
        var bestScore = int.MinValue;
        for (var i = 0; i < masterWeaponSkins.Length; i++)
        {
            var candidate = masterWeaponSkins[i];
            if (candidate == null || !IsCompatibleWeaponSkin(candidate, character, controller._mCharacterId))
            {
                continue;
            }

            if (preferredSkinId > 0 && candidate.id == preferredSkinId)
            {
                return candidate;
            }

            var score = 0;
            if (candidate.m_character_id == controller._mCharacterId)
            {
                score += 4;
            }

            if (candidate.is_default != 0)
            {
                score += 8;
            }

            if (best == null || score > bestScore ||
                (score == bestScore && candidate.display_order < best.display_order) ||
                (score == bestScore && candidate.display_order == best.display_order && candidate.id < best.id))
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static bool IsCompatibleWeaponSkin(
        MWeaponSkins weaponSkin,
        MCharacters character,
        long characterId)
    {
        return weaponSkin.m_character_id == characterId ||
               (weaponSkin.m_character_id == 0 && weaponSkin.weapon_type == character.weapon_type);
    }

    private static bool ContainsWeaponSkinEntitlement(
        Il2CppWeaponSkinDataList rows,
        long skinId)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row != null && row._MWeaponSkinId_k__BackingField == skinId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSelectedWeaponSkin(Il2CppWeaponSkinCellList? cells)
    {
        if (cells == null)
        {
            return false;
        }

        for (var i = 0; i < cells.Count; i++)
        {
            if (cells[i]?.IsSelected == true)
            {
                return true;
            }
        }

        return false;
    }

    private static void UnlockMatchingWeaponSkin(
        Il2CppWeaponSkinCellList cells,
        long skinId)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell?.ThumbnailModel?.ContentId != skinId)
            {
                continue;
            }

            cell.IsLocked = false;
            cell.IsEquipped = true;
            cell.IsSelected = true;
            return;
        }
    }

    private static WeaponSkinCellModel CreateWeaponSkinCellModel(
        SkinChangePopupController controller,
        MWeaponSkins master)
    {
        return new WeaponSkinCellModel
        {
            Name = master.name ?? string.Empty,
            Description = master.description ?? string.Empty,
            AssetId = master.asset_id ?? string.Empty,
            IsDefault = master.is_default,
            IsLocked = false,
            IsEquipped = true,
            IsSelected = true,
            IsPaid = master.is_paid,
            DisplayOrder = master.display_order,
            ReleasedAt = Project.DateTimeUtility.ConvertToDateTimeOrDefault(master.released_at),
            ThumbnailModel = WeaponSkinThumbnailModel.Create(master.id, controller._cancellationToken)
        };
    }

    private static List<long> CollectAvailableSkinIds(Il2CppSkinCellList? cells)
    {
        var result = new List<long>(cells?.Count ?? 0);
        if (cells == null)
        {
            return result;
        }

        for (var i = 0; i < cells.Count; i++)
        {
            var thumbnail = cells[i]?.ThumbnailModel;
            if (thumbnail != null && thumbnail.ContentId > 0)
            {
                result.Add(thumbnail.ContentId);
            }
        }

        return result;
    }

    private static long FindSelectedSkinId(Il2CppSkinCellList? cells, long fallback)
    {
        if (cells == null)
        {
            return fallback;
        }

        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            var thumbnail = cell?.ThumbnailModel;
            if (cell != null && cell.IsSelected && thumbnail != null && thumbnail.ContentId > 0)
            {
                return thumbnail.ContentId;
            }
        }

        return fallback;
    }

    private static void RemoveCharacterInjection(CharacterInjectionState? state)
    {
        if (state?.List != null && state.Character != null)
        {
            state.List.Remove(state.Character);
        }
    }

    private static void RemoveEntitlementInjection(EntitlementInjectionState? state)
    {
        if (state?.List == null)
        {
            return;
        }

        for (var i = state.Added.Count - 1; i >= 0; i--)
        {
            state.List.Remove(state.Added[i]);
        }

        state.Added.Clear();
    }

    private static void RemoveWeaponEntitlementInjection(WeaponEntitlementInjectionState? state)
    {
        if (state?.List != null && state.Added != null)
        {
            state.List.Remove(state.Added);
        }
    }
}
