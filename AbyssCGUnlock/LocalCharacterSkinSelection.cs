#nullable enable

using System;
using System.Collections.Generic;

namespace AbyssCGUnlock;

internal readonly struct CharacterSkinSelection : IEquatable<CharacterSkinSelection>
{
    internal CharacterSkinSelection(long battleSkinId, long tavernSkinId)
    {
        BattleSkinId = battleSkinId;
        TavernSkinId = tavernSkinId;
    }

    internal long BattleSkinId { get; }
    internal long TavernSkinId { get; }

    public bool Equals(CharacterSkinSelection other)
    {
        return BattleSkinId == other.BattleSkinId && TavernSkinId == other.TavernSkinId;
    }

    public override bool Equals(object? obj)
    {
        return obj is CharacterSkinSelection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BattleSkinId, TavernSkinId);
    }
}

internal readonly struct CharacterSkinPresentationCandidate
{
    internal CharacterSkinPresentationCandidate(
        long skinId,
        long characterId,
        int skinType,
        string assetId)
    {
        SkinId = skinId;
        CharacterId = characterId;
        SkinType = skinType;
        AssetId = assetId;
    }

    internal long SkinId { get; }
    internal long CharacterId { get; }
    internal int SkinType { get; }
    internal string AssetId { get; }
}

internal readonly struct CharacterDetailPresentationPlan
{
    internal CharacterDetailPresentationPlan(
        string assetId,
        bool useProjectedCharacterData)
    {
        DetailAssetId = assetId;
        CharacterModelAssetId = assetId;
        UseProjectedCharacterData = useProjectedCharacterData;
    }

    internal string DetailAssetId { get; }
    internal string CharacterModelAssetId { get; }
    internal bool UseProjectedCharacterData { get; }
}

internal static class CharacterDetailPresentationPolicy
{
    internal static CharacterDetailPresentationPlan ResolveMasterReadOnly(
        string masterAssetId,
        string projectedAssetId)
    {
        if (!string.IsNullOrEmpty(projectedAssetId))
        {
            return new CharacterDetailPresentationPlan(
                projectedAssetId,
                useProjectedCharacterData: true);
        }

        return new CharacterDetailPresentationPlan(
            masterAssetId ?? string.Empty,
            useProjectedCharacterData: false);
    }
}

internal static class CharacterSkinPresentationPolicy
{
    private const int BattleSkinType = 1;

    internal static string ResolveBattleAssetId(
        long characterId,
        long battleSkinId,
        string currentAssetId,
        IEnumerable<CharacterSkinPresentationCandidate> candidates)
    {
        if (characterId <= 0 || battleSkinId <= 0 || candidates == null)
        {
            return currentAssetId;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.CharacterId == characterId &&
                candidate.SkinId == battleSkinId &&
                candidate.SkinType == BattleSkinType &&
                !string.IsNullOrEmpty(candidate.AssetId))
            {
                return candidate.AssetId;
            }
        }

        return currentAssetId;
    }
}

internal static class CharacterThumbnailSelectionPolicy
{
    internal static long ResolveExactSkinId(
        CharacterSkinSelection selection,
        bool isTavern)
    {
        var skinId = isTavern
            ? selection.TavernSkinId
            : selection.BattleSkinId;

        return skinId > 0 ? skinId : 0;
    }
}

internal sealed class AccountScopedCharacterSkinSelections<TAccountKey>
    where TAccountKey : notnull
{
    private readonly object _gate = new();
    private readonly Dictionary<TAccountKey, Dictionary<long, CharacterSkinSelection>> _selections = new();

    internal void Save(TAccountKey accountKey, long characterId, CharacterSkinSelection selection)
    {
        lock (_gate)
        {
            if (!_selections.TryGetValue(accountKey, out var accountSelections))
            {
                accountSelections = new Dictionary<long, CharacterSkinSelection>();
                _selections.Add(accountKey, accountSelections);
            }

            accountSelections[characterId] = selection;
        }
    }

    internal bool TryGet(TAccountKey accountKey, long characterId, out CharacterSkinSelection selection)
    {
        lock (_gate)
        {
            if (_selections.TryGetValue(accountKey, out var accountSelections) &&
                accountSelections.TryGetValue(characterId, out selection))
            {
                return true;
            }

            selection = default;
            return false;
        }
    }
}

internal sealed class AccountScopedCharacterSkinEntitlements<TAccountKey>
    where TAccountKey : notnull
{
    private readonly object _gate = new();
    private readonly Dictionary<TAccountKey, Dictionary<long, HashSet<long>>> _skinIds = new();

    internal void Register(
        TAccountKey accountKey,
        long characterId,
        IEnumerable<long> skinIds)
    {
        lock (_gate)
        {
            if (!_skinIds.TryGetValue(accountKey, out var accountSkinIds))
            {
                accountSkinIds = new Dictionary<long, HashSet<long>>();
                _skinIds.Add(accountKey, accountSkinIds);
            }

            if (!accountSkinIds.TryGetValue(characterId, out var characterSkinIds))
            {
                characterSkinIds = new HashSet<long>();
                accountSkinIds.Add(characterId, characterSkinIds);
            }

            foreach (var skinId in skinIds)
            {
                if (skinId > 0)
                {
                    characterSkinIds.Add(skinId);
                }
            }
        }
    }

    internal bool Contains(TAccountKey accountKey, long characterId, long skinId)
    {
        lock (_gate)
        {
            return skinId > 0 &&
                   _skinIds.TryGetValue(accountKey, out var accountSkinIds) &&
                   accountSkinIds.TryGetValue(characterId, out var characterSkinIds) &&
                   characterSkinIds.Contains(skinId);
        }
    }
}

internal static class DynamicCharacterSkinEntitlementPlanner
{
    internal static IReadOnlyList<long> Plan(
        IEnumerable<long> candidateSkinIds,
        IEnumerable<long> ownedSkinIds)
    {
        var owned = new HashSet<long>(ownedSkinIds);
        var planned = new List<long>();
        var seen = new HashSet<long>();

        foreach (var skinId in candidateSkinIds)
        {
            if (skinId > 0 && !owned.Contains(skinId) && seen.Add(skinId))
            {
                planned.Add(skinId);
            }
        }

        return planned;
    }
}

internal static class CharacterSkinSelectionPolicy
{
    internal static CharacterSkinSelection Resolve(
        CharacterSkinSelection current,
        CharacterSkinSelection selected,
        IEnumerable<long> availableBattleSkinIds,
        IEnumerable<long> availableTavernSkinIds)
    {
        var availableBattle = new HashSet<long>(availableBattleSkinIds);
        var availableTavern = new HashSet<long>(availableTavernSkinIds);

        return new CharacterSkinSelection(
            selected.BattleSkinId > 0 && availableBattle.Contains(selected.BattleSkinId)
                ? selected.BattleSkinId
                : current.BattleSkinId,
            selected.TavernSkinId > 0 && availableTavern.Contains(selected.TavernSkinId)
                ? selected.TavernSkinId
                : current.TavernSkinId);
    }

    internal static bool ShouldHandleLocally(
        bool isSyntheticCharacter,
        CharacterSkinSelection current,
        CharacterSkinSelection selected,
        bool requiresLocalEntitlement)
    {
        return isSyntheticCharacter || requiresLocalEntitlement || !current.Equals(selected);
    }
}

internal static class CharacterSkinEntitlementPolicy
{
    internal static bool RequiresLocalEntitlement(
        bool isOwned,
        bool masterRowExists,
        bool isDefaultSkin)
    {
        return !isOwned && (!masterRowExists || !isDefaultSkin);
    }
}

internal static class CharacterSkinModelSelectionPolicy
{
    internal static long ResolveCurrentSkinId(
        long currentSkinId,
        IEnumerable<(long SkinId, bool IsDefault, int DisplayOrder)> candidates)
    {
        var hasFallback = false;
        var fallbackId = 0L;
        var fallbackOrder = int.MaxValue;
        var fallbackIsDefault = false;

        foreach (var candidate in candidates)
        {
            if (candidate.SkinId <= 0)
            {
                continue;
            }

            if (candidate.SkinId == currentSkinId)
            {
                return currentSkinId;
            }

            if (!hasFallback ||
                (candidate.IsDefault && !fallbackIsDefault) ||
                (candidate.IsDefault == fallbackIsDefault &&
                 candidate.DisplayOrder < fallbackOrder) ||
                (candidate.IsDefault == fallbackIsDefault &&
                 candidate.DisplayOrder == fallbackOrder &&
                 candidate.SkinId < fallbackId))
            {
                hasFallback = true;
                fallbackId = candidate.SkinId;
                fallbackOrder = candidate.DisplayOrder;
                fallbackIsDefault = candidate.IsDefault;
            }
        }

        return hasFallback ? fallbackId : 0;
    }
}

internal static class SkinChangePopupCompletenessPolicy
{
    internal static bool NeedsSyntheticWeaponFallback(
        bool isSyntheticCharacter,
        int weaponSkinCellCount)
    {
        return isSyntheticCharacter && weaponSkinCellCount == 0;
    }
}
