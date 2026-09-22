using System;
using System.Collections.Generic;
using Absf;
using Project.Master;
using Project.Master.NoaMessagePack;
using Project.User;

namespace AbyssCGUnlock;

/// <summary>
/// Rebuilds the unowned snapshot from the downloaded master cache and the active UserData.
/// Every master character is a candidate, including rows the client public list hides with open_at.
/// Synthesis runs only when the current cache contains the rows Apply and AdventurerDetail look up
/// with First or FirstSafe. One bad row is skipped and does not abort the refresh.
/// </summary>
internal static class DynamicCharacterCatalog
{
    private static bool _loggedCreateFailure;

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
        var masterCharacters = masterDataStore.GetCache<MCharacters>();
        var masterCharacterSkins = masterDataStore.GetCache<MCharacterSkins>();
        var masterTavernCharacterCards = masterDataStore.GetCache<MTavernCharacterCards>();
        var masterProfiles = masterDataStore.GetCache<MCharacterProfiles>();
        var masterUnions = masterDataStore.GetCache<MUnionTypes>();
        var candidateIds = new List<long>(masterCharacters.Length);
        var unionByCharacter = new Dictionary<long, long>(masterCharacters.Length);
        var skinCandidates =
            new List<(long SkinId, long CharacterId, int Type, int IsDefault)>(masterCharacterSkins.Length);
        var tavernCardPairs =
            new List<(long CharacterId, long SkinId)>(masterTavernCharacterCards.Length);
        var unionIds = new HashSet<long>();
        var profileIds = new HashSet<long>();
        var battleAssetReady = new HashSet<long>();
        var seenBattleSkin = new HashSet<long>();

        for (var i = 0; i < masterCharacters.Length; i++)
        {
            var masterCharacter = masterCharacters[i];
            if (masterCharacter == null)
            {
                continue;
            }

            candidateIds.Add(masterCharacter.id);
            unionByCharacter[masterCharacter.id] = masterCharacter.union_type;
        }

        for (var i = 0; i < masterUnions.Length; i++)
        {
            var union = masterUnions[i];
            if (union != null)
            {
                unionIds.Add(union.id);
            }
        }

        for (var i = 0; i < masterProfiles.Length; i++)
        {
            var profile = masterProfiles[i];
            if (profile != null)
            {
                profileIds.Add(profile.m_character_id);
            }
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

            // Apply takes the first type=1 default skin. An empty asset id on that row is not usable.
            if (masterSkin.type == 1 &&
                masterSkin.is_default == 1 &&
                seenBattleSkin.Add(masterSkin.m_character_id) &&
                !string.IsNullOrEmpty(masterSkin.asset_id))
            {
                battleAssetReady.Add(masterSkin.m_character_id);
            }
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

        var selectedIds = DynamicUnownedCharacterSelector.Select(candidateIds, ownedMasterIds);
        var tavernSkinIds = DynamicTavernCharacterSkinSelector.Select(skinCandidates, tavernCardPairs);
        var syntheticCharacters = new List<CharacterData>(selectedIds.Count);
        long firstMappedCharacterId = 0;
        long firstMappedSkinId = 0;
        var skipped = 0;

        for (var i = 0; i < selectedIds.Count; i++)
        {
            var characterId = selectedIds[i];
            var hasUnion = unionByCharacter.TryGetValue(characterId, out var unionType) &&
                           unionIds.Contains(unionType);
            if (!tavernSkinIds.TryGetValue(characterId, out var tavernSkinId) ||
                !DynamicUnownedCharacterSelector.CanLocallyUnlock(
                    hasUnion,
                    battleAssetReady.Contains(characterId),
                    profileIds.Contains(characterId),
                    hasTavernWorkSkin: true))
            {
                skipped++;
                continue;
            }

            try
            {
                var character = CharacterDataStore.CreateFromMaster(characterId, 1, 0);
                if (character == null)
                {
                    skipped++;
                    continue;
                }

                // Keep the native detail page out of its IsNew-only network branch.
                character._IsNew_k__BackingField = false;
                character._IsExistStory_k__BackingField = true;
                character._TavernMCharacterSkinId_k__BackingField = tavernSkinId;
                LocalCharacterSkinRegistry.ApplySavedSelection(
                    userData,
                    characterId,
                    character,
                    masterDataStore);
                syntheticCharacters.Add(character);

                if (firstMappedSkinId == 0)
                {
                    firstMappedCharacterId = characterId;
                    firstMappedSkinId = tavernSkinId;
                }
            }
            catch (Exception exception)
            {
                skipped++;
                if (!_loggedCreateFailure)
                {
                    _loggedCreateFailure = true;
                    CgUnlockPlugin.LogSource.LogWarning(
                        $"[CGUnlock] 角色合成失败已跳过，后续同类失败只计数: id={characterId}, error={exception.GetType().Name}");
                }
            }
        }

        LocalCharacterRegistry.Replace(userData, syntheticCharacters);
        if (syntheticCharacters.Count > 0)
        {
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 未持有角色交流皮肤Master映射已校正: mapped={syntheticCharacters.Count}, " +
                $"skipped={skipped}, first_character_id={firstMappedCharacterId}, first_skin_id={firstMappedSkinId}");
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 未持有角色酒馆卡片Master兼容已启用: mapped={syntheticCharacters.Count}, " +
                $"first_character_id={firstMappedCharacterId}, first_skin_id={firstMappedSkinId}, " +
                "tavern_card_backed=true");
        }
        else if (skipped > 0)
        {
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 未持有角色均因客户端资源不完整跳过: skipped={skipped}");
        }

        return syntheticCharacters;
    }
}
