#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Absf;
using BepInEx;
using Il2CppInterop.Runtime;
using Project.Master;
using Project.Master.NoaMessagePack;
using Project.User;

namespace AbyssCGUnlock;

/// <summary>
/// Keeps confirmed character-skin selections in managed memory and the plugin cache. Values are
/// isolated by account and character; no Unity or IL2CPP object is retained on disk.
/// </summary>
internal static class LocalCharacterSkinRegistry
{
    private readonly struct AccountKey : IEquatable<AccountKey>
    {
        internal AccountKey(bool isServerUserId, long value)
        {
            IsServerUserId = isServerUserId;
            Value = value;
        }

        private bool IsServerUserId { get; }
        private long Value { get; }
        internal bool IsValid => Value != 0;

        public bool Equals(AccountKey other)
        {
            return IsServerUserId == other.IsServerUserId && Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is AccountKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsServerUserId, Value);
        }
    }

    private static readonly AccountScopedCharacterSkinSelections<AccountKey> Selections = new();
    private static readonly AccountScopedCharacterSkinEntitlements<AccountKey> LocalEntitlements = new();
    private static readonly object PersistentCacheGate = new();
    private static CharacterSkinSelectionDiskCache? _persistentCache;
    private static bool _loggedFirstPersistentRestore;
    private static bool _loggedPersistentWriteFailure;

    internal static bool TryGet(
        UserData? userData,
        long characterId,
        out CharacterSkinSelection selection)
    {
        var accountKey = ResolveAccountKey(userData);
        if (!accountKey.IsValid)
        {
            selection = default;
            return false;
        }

        if (Selections.TryGet(accountKey, characterId, out selection))
        {
            return true;
        }

        if (!TryResolveStableUserId(userData, out var userId) ||
            !TryGetPersistentSelection(userId, characterId, out selection))
        {
            return false;
        }

        Selections.Save(accountKey, characterId, selection);
        if (!_loggedFirstPersistentRestore)
        {
            _loggedFirstPersistentRestore = true;
            CgUnlockPlugin.LogSource?.LogInfo(
                $"[CGUnlock] 本地换装磁盘缓存已恢复: first_character_id={characterId}");
        }

        return true;
    }

    internal static bool Save(
        UserData? userData,
        long characterId,
        CharacterSkinSelection selection)
    {
        var accountKey = ResolveAccountKey(userData);
        if (!accountKey.IsValid || characterId <= 0)
        {
            return false;
        }

        Selections.Save(accountKey, characterId, selection);
        if (TryResolveStableUserId(userData, out var userId) &&
            !TrySavePersistentSelection(userId, characterId, selection) &&
            !_loggedPersistentWriteFailure)
        {
            _loggedPersistentWriteFailure = true;
            CgUnlockPlugin.LogSource?.LogWarning(
                "[CGUnlock] 本地换装磁盘缓存写入失败；本次客户端会话状态仍然有效。");
        }

        return true;
    }

    internal static bool ApplySavedSelection(
        UserData? userData,
        long characterId,
        MasterDataStore? masterDataStore = null)
    {
        if (!TryGet(userData, characterId, out var selection) ||
            !TryFindCharacter(userData, characterId, out var character))
        {
            return false;
        }

        Apply(character, characterId, selection, masterDataStore);
        return true;
    }

    internal static bool ApplySavedSelection(
        UserData? userData,
        long characterId,
        CharacterData? character,
        MasterDataStore? masterDataStore = null)
    {
        if (character == null || !TryGet(userData, characterId, out var selection))
        {
            return false;
        }

        Apply(character, characterId, selection, masterDataStore);
        return true;
    }

    internal static bool ApplySelection(
        UserData? userData,
        long characterId,
        CharacterSkinSelection selection,
        MasterDataStore? masterDataStore = null)
    {
        if (!TryFindCharacter(userData, characterId, out var character))
        {
            return false;
        }

        Apply(character, characterId, selection, masterDataStore);
        return true;
    }

    internal static bool IsSyntheticCharacter(UserData? userData, long characterId)
    {
        return userData != null &&
               LocalCharacterRegistry.TryGet(userData, characterId, out var character) &&
               character != null;
    }

    internal static void RegisterLocalEntitlements(
        UserData? userData,
        long characterId,
        IEnumerable<long> skinIds)
    {
        var accountKey = ResolveAccountKey(userData);
        if (!accountKey.IsValid || characterId <= 0)
        {
            return;
        }

        LocalEntitlements.Register(accountKey, characterId, skinIds);
    }

    internal static bool IsLocallyUnlocked(
        UserData? userData,
        long characterId,
        CharacterSkinSelection selection)
    {
        var accountKey = ResolveAccountKey(userData);
        return accountKey.IsValid &&
               (LocalEntitlements.Contains(accountKey, characterId, selection.BattleSkinId) ||
                LocalEntitlements.Contains(accountKey, characterId, selection.TavernSkinId));
    }

    private static void Apply(
        CharacterData character,
        long characterId,
        CharacterSkinSelection selection,
        MasterDataStore? masterDataStore)
    {
        var current = new CharacterSkinSelection(
            character._BattleMCharacterSkinId_k__BackingField,
            character._TavernMCharacterSkinId_k__BackingField);
        var plan = CharacterSkinRestartRestorePolicy.Resolve(
            characterId,
            current,
            character._AssetId_k__BackingField ?? string.Empty,
            selection,
            Array.Empty<CharacterSkinPresentationCandidate>());

        try
        {
            var masterSkins = (masterDataStore ?? Engine.Get<MasterDataStore>())?
                .GetCache<MCharacterSkins>();
            if (masterSkins != null)
            {
                var candidates = new List<CharacterSkinPresentationCandidate>(masterSkins.Length);
                for (var i = 0; i < masterSkins.Length; i++)
                {
                    var masterSkin = masterSkins[i];
                    if (masterSkin == null || masterSkin.m_character_id != characterId)
                    {
                        continue;
                    }

                    candidates.Add(new CharacterSkinPresentationCandidate(
                        masterSkin.id,
                        masterSkin.m_character_id,
                        masterSkin.type,
                        masterSkin.asset_id ?? string.Empty));
                }

                plan = CharacterSkinRestartRestorePolicy.Resolve(
                    characterId,
                    current,
                    character._AssetId_k__BackingField ?? string.Empty,
                    selection,
                    candidates);
            }
        }
        catch
        {
            // The skin IDs remain process-local even if MasterDataStore is between lifecycle states.
            // A later detail/popup refresh reapplies the same selection and presentation projection.
        }

        character._BattleMCharacterSkinId_k__BackingField = plan.Selection.BattleSkinId;
        character._TavernMCharacterSkinId_k__BackingField = plan.Selection.TavernSkinId;
        character._AssetId_k__BackingField = plan.AssetId;
    }

    private static bool TryFindCharacter(
        UserData? userData,
        long characterId,
        out CharacterData character)
    {
        var ownedRows = userData?._CharaDataStore_k__BackingField?._dataList;
        if (ownedRows != null)
        {
            for (var i = 0; i < ownedRows.Count; i++)
            {
                var row = ownedRows[i];
                if (row != null &&
                    (row.MCharaId == characterId || row._Id_k__BackingField == characterId))
                {
                    character = row;
                    return true;
                }
            }
        }

        if (userData != null &&
            LocalCharacterRegistry.TryGet(userData, characterId, out var registered) &&
            registered != null)
        {
            character = registered;
            return true;
        }

        character = null!;
        return false;
    }

    private static AccountKey ResolveAccountKey(UserData? userData)
    {
        if (userData == null)
        {
            return default;
        }

        try
        {
            if (TryResolveStableUserId(userData, out var userId))
            {
                return new AccountKey(true, userId);
            }
        }
        catch
        {
            // Account bootstrap can expose UserData before UserStatus. The native object identity
            // is an account-local fallback until the stable user ID is available.
        }

        try
        {
            return new AccountKey(
                false,
                IL2CPP.Il2CppObjectBaseToPtr(userData).ToInt64());
        }
        catch
        {
            return default;
        }
    }

    private static bool TryResolveStableUserId(UserData? userData, out long userId)
    {
        try
        {
            userId = userData?._UserStatus_k__BackingField?._UserId_k__BackingField ?? 0;
            return userId > 0;
        }
        catch
        {
            userId = 0;
            return false;
        }
    }

    private static CharacterSkinSelectionDiskCache GetPersistentCache()
    {
        lock (PersistentCacheGate)
        {
            return _persistentCache ??= new CharacterSkinSelectionDiskCache(
                Path.Combine(
                    Paths.CachePath,
                    PluginInfo.PluginName,
                    "skin-selections.v1.cache"));
        }
    }

    private static bool TryGetPersistentSelection(
        long userId,
        long characterId,
        out CharacterSkinSelection selection)
    {
        try
        {
            return GetPersistentCache().TryGet(userId, characterId, out selection);
        }
        catch
        {
            selection = default;
            return false;
        }
    }

    private static bool TrySavePersistentSelection(
        long userId,
        long characterId,
        CharacterSkinSelection selection)
    {
        try
        {
            return GetPersistentCache().Save(userId, characterId, selection);
        }
        catch
        {
            return false;
        }
    }
}
