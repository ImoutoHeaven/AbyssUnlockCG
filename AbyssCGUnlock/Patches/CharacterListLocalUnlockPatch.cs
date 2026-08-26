using System.Reflection;
using HarmonyLib;
using Project.Interaction.CharacterList;
using CharacterListSubService = Project.Interaction.CharacterList.SubService;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Refreshes the local registry from the game's current dynamic unowned-character list.
/// The list is produced from the downloaded MCharacters cache and current account ownership.
/// </summary>
internal static class CharacterCatalogCapturePatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CharacterListSubService),
            nameof(CharacterListSubService.CreateCharacterThumbnailModels));
    }

    internal static void Postfix(CharacterListSubService __instance)
    {
        if (__instance == null)
        {
            return;
        }

        if (!PluginConfig.EnableUnownedCharacterDetail.Value)
        {
            LocalCharacterRegistry.Clear(__instance._userData);
            return;
        }

        var count = LocalCharacterRegistry.Replace(__instance._userData, __instance._nonHasCharaDataList);
        CgUnlockPlugin.LogSource.LogDebug($"[CGUnlock] 当前账号动态未持有角色目录已替换: count={count}");
    }
}

/// <summary>
/// Replays the native selection callback for a dynamically discovered unowned character.
/// It only publishes the local character ID; ownership flags and account data stay untouched.
/// </summary>
internal static class UnownedCharacterSelectionPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CharacterListSubService.__c),
            nameof(CharacterListSubService.__c._SetupCharacterListEvent_b__33_0),
            new[] { typeof(CharacterInteractionThumbnailModel), typeof(CharacterListSubService) });
    }

    internal static bool Prefix(CharacterInteractionThumbnailModel model, CharacterListSubService subService)
    {
        if (!PluginConfig.EnableUnownedCharacterDetail.Value || model == null || subService == null)
        {
            return true;
        }

        // Owned characters retain the original callback and all native behavior.
        if (model._IsHasCharacter_k__BackingField)
        {
            return true;
        }

        var characterModel = model._CharacterModel_k__BackingField;
        if (characterModel == null)
        {
            return true;
        }

        var characterId = characterModel.TCharacterId;
        if (!LocalCharacterRegistry.TryGet(subService._userData, characterId, out _))
        {
            return true;
        }

        if (subService._resultSubject == null)
        {
            return true;
        }

        subService._resultSubject.OnNext(characterId);
        CgUnlockPlugin.LogSource.LogInfo($"[CGUnlock] 未持有角色详情已本地放行: characterId={characterId}");
        return false;
    }
}
