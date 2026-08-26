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

        var cardBackedPairs = new HashSet<(long CharacterId, long SkinId)>(tavernCardPairs);
        var selected = new Dictionary<long, long>();

        foreach (var skin in skins)
        {
            if (skin.Type != TavernSkinType ||
                skin.IsDefault != DefaultFlag ||
                !cardBackedPairs.Contains((skin.CharacterId, skin.SkinId)) ||
                selected.ContainsKey(skin.CharacterId))
            {
                continue;
            }

            selected.Add(skin.CharacterId, skin.SkinId);
        }

        return selected;
    }
}
