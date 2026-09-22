using System;
using System.Collections.Generic;

namespace AbyssCGUnlock;

/// <summary>
/// Selects the default work skin that is actually backed by a Tavern character-card row.
/// CharacterData.Apply(masterId) initializes the ordinary type-1 skin only, while
/// AdventurerDetail requires the card-backed type-2 skin in TavernMCharacterSkinId.
/// </summary>
internal static class DynamicTavernCharacterSkinSelector
{
    private const int TavernSkinType = 2;
    private const int DefaultFlag = 1;

    internal static IReadOnlyDictionary<long, long> Select(
        IEnumerable<(long SkinId, long CharacterId, int Type, int IsDefault)> skins,
        IEnumerable<(long CharacterId, long SkinId)> tavernCardPairs)
    {
        if (skins == null)
        {
            throw new ArgumentNullException(nameof(skins));
        }

        if (tavernCardPairs == null)
        {
            throw new ArgumentNullException(nameof(tavernCardPairs));
        }

        var pairs = new List<(long CharacterId, long SkinId)>(tavernCardPairs);
        var cardBackedPairs = new HashSet<(long CharacterId, long SkinId)>(pairs);
        var skinOwner = new Dictionary<long, long>();
        var selected = new Dictionary<long, long>();

        foreach (var skin in skins)
        {
            if (!skinOwner.ContainsKey(skin.SkinId))
            {
                skinOwner.Add(skin.SkinId, skin.CharacterId);
            }

            if (skin.Type != TavernSkinType ||
                skin.IsDefault != DefaultFlag ||
                !cardBackedPairs.Contains((skin.CharacterId, skin.SkinId)) ||
                selected.ContainsKey(skin.CharacterId))
            {
                continue;
            }

            selected.Add(skin.CharacterId, skin.SkinId);
        }

        // A future card may not use the type-2 default. The detail page matches the card row, not the type.
        foreach (var pair in pairs)
        {
            if (selected.ContainsKey(pair.CharacterId) ||
                !skinOwner.TryGetValue(pair.SkinId, out var owner) ||
                owner != pair.CharacterId)
            {
                continue;
            }

            selected.Add(pair.CharacterId, pair.SkinId);
        }

        return selected;
    }
}
