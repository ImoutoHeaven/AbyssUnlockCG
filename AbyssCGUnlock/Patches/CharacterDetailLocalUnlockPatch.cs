using System.Reflection;
using HarmonyLib;
using Project.User;
using DetailSubService = Project.Interaction.AdventurerDetail.SubService;
using Il2CppCharacterDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterData>;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Temporarily injects one dynamic unowned CharacterData while UpdateView synchronously resolves it.
/// The Finalizer removes it immediately, so the account's real ownership list is never persisted.
/// </summary>
internal static class CharacterDetailLocalUnlockPatch
{
    internal readonly struct InjectionState
    {
        internal InjectionState(Il2CppCharacterDataList list, CharacterData character)
        {
            List = list;
            Character = character;
        }

        internal Il2CppCharacterDataList List { get; }
        internal CharacterData Character { get; }
        internal bool Added => List != null && Character != null;
    }

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(DetailSubService),
            nameof(DetailSubService.UpdateView),
            new[] { typeof(long) });
    }

    internal static void Prefix(DetailSubService __instance, long tCharacterId, out InjectionState __state)
    {
        __state = default;

        if (!PluginConfig.EnableUnownedCharacterDetail.Value || __instance == null)
        {
            return;
        }

        var userData = __instance._userData;
        if (PluginConfig.EnableLocalCharacterSkinChange.Value)
        {
            LocalCharacterSkinRegistry.ApplySavedSelection(
                userData,
                tCharacterId,
                __instance._masterDataStore);
        }

        if (!LocalCharacterRegistry.TryGet(userData, tCharacterId, out var character) || character == null)
        {
            return;
        }

        var list = userData?._CharaDataStore_k__BackingField?._dataList;
        if (list == null)
        {
            return;
        }

        // Let a real owned entry win if account state changed between list click and detail open.
        for (var i = 0; i < list.Count; i++)
        {
            var current = list[i];
            if (current != null && current._Id_k__BackingField == tCharacterId)
            {
                return;
            }
        }

        character._IsNew_k__BackingField = false;
        character._IsExistStory_k__BackingField = true;
        list.Add(character);
        __state = new InjectionState(list, character);
    }

    internal static System.Exception Finalizer(System.Exception __exception, InjectionState __state)
    {
        if (__state.Added)
        {
            __state.List.Remove(__state.Character);
        }

        return __exception;
    }
}
