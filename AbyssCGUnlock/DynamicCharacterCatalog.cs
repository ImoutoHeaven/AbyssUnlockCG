using System;
using System.Collections.Generic;
using Absf;
using Project.Master;
using Project.Master.NoaMessagePack;
using Project.User;

namespace AbyssCGUnlock;

/// <summary>
/// Rebuilds the unowned-character snapshot from the game's downloaded master cache and
/// the currently active UserData. It intentionally carries no character IDs of its own.
/// </summary>
internal static class DynamicCharacterCatalog
{
    internal static IReadOnlyList<CharacterData> Refresh(UserData userData)
    {
        if (userData == null)
        {
            return Array.Empty<CharacterData>();
        }

        var characterStore = userData._CharaDataStore_k__BackingField;
        var ownedCharacters = characterStore?._dataList;
        var ownedMasterIds = new HashSet<long>();

        if (ownedCharacters != null)
        {
            for (var i = 0; i < ownedCharacters.Count; i++)
            {
                var character = ownedCharacters[i];
                if (character != null)
                {
                    ownedMasterIds.Add(character.MCharaId);
                }
            }
        }

        var masterDataStore = Engine.Get<MasterDataStore>();
        var serverTimeAccessor = Engine.Get<IServerTimeAccessor>();
        var now = serverTimeAccessor.NowTime;
        var masterCharacters = masterDataStore.GetCache<MCharacters>();
        var masterCharacterSkins = masterDataStore.GetCache<MCharacterSkins>();
        var masterTavernCharacterCards = masterDataStore.GetCache<MTavernCharacterCards>();
        var candidates = new List<KeyValuePair<long, bool>>(masterCharacters.Length);
        var skinCandidates =
            new List<(long SkinId, long CharacterId, int Type, int IsDefault)>(masterCharacterSkins.Length);
        var tavernCardPairs =
            new List<(long CharacterId, long SkinId)>(masterTavernCharacterCards.Length);

        for (var i = 0; i < masterCharacters.Length; i++)
        {
            var masterCharacter = masterCharacters[i];
            if (masterCharacter == null)
            {
                continue;
            }

            var isReleased = Project.DateTimeExtensions.IsBetween(now, masterCharacter.open_at, null);
            candidates.Add(new KeyValuePair<long, bool>(masterCharacter.id, isReleased));
        }

        for (var i = 0; i < masterCharacterSkins.Length; i++)
        {
            var masterSkin = masterCharacterSkins[i];
            if (masterSkin == null)
            {
                continue;
            }

            skinCandidates.Add((
                masterSkin.id,
                masterSkin.m_character_id,
                masterSkin.type,
                masterSkin.is_default));
        }

        for (var i = 0; i < masterTavernCharacterCards.Length; i++)
        {
            var tavernCard = masterTavernCharacterCards[i];
            if (tavernCard == null)
            {
                continue;
            }

            tavernCardPairs.Add((
                tavernCard.m_character_id,
                tavernCard.m_character_skin_id));
        }

        var selectedIds = DynamicUnownedCharacterSelector.Select(candidates, ownedMasterIds);
        var tavernSkinIds = DynamicTavernCharacterSkinSelector.Select(skinCandidates, tavernCardPairs);
        var syntheticCharacters = new List<CharacterData>(selectedIds.Count);
        long firstMappedCharacterId = 0;
        long firstMappedSkinId = 0;

        for (var i = 0; i < selectedIds.Count; i++)
        {
            if (!tavernSkinIds.TryGetValue(selectedIds[i], out var tavernSkinId))
            {
                // A future character without a default work skin backed by a Tavern-card row
                // cannot enter AdventurerDetail safely, so omit it for this refresh.
                continue;
            }

            var character = CharacterDataStore.CreateFromMaster(selectedIds[i], 1, 0);
            if (character == null)
            {
                continue;
            }

            // Keep the native detail page out of its IsNew-only network branch.
            character._IsNew_k__BackingField = false;
            character._IsExistStory_k__BackingField = true;
            character._TavernMCharacterSkinId_k__BackingField = tavernSkinId;
            syntheticCharacters.Add(character);

            if (firstMappedSkinId == 0)
            {
                firstMappedCharacterId = selectedIds[i];
                firstMappedSkinId = tavernSkinId;
            }
        }

        LocalCharacterRegistry.Replace(userData, syntheticCharacters);
        if (syntheticCharacters.Count > 0)
        {
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 未持有角色交流皮肤Master映射已校正: mapped={syntheticCharacters.Count}, " +
                $"first_character_id={firstMappedCharacterId}, first_skin_id={firstMappedSkinId}");
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 未持有角色酒馆卡片Master兼容已启用: mapped={syntheticCharacters.Count}, " +
                $"first_character_id={firstMappedCharacterId}, first_skin_id={firstMappedSkinId}, " +
                "tavern_card_backed=true");
        }

        return syntheticCharacters;
    }
}
