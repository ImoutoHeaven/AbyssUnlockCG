using System;
using System.Reflection;
using HarmonyLib;
using Project.User;
using DetailSubService = Project.Interaction.AdventurerDetail.SubService;
using UpdateViewStateMachine = Project.Interaction.AdventurerDetail.SubService._UpdateView_d__101;
using Il2CppCharacterDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterData>;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Temporarily injects one dynamic unowned CharacterData while UpdateView resolves it.
/// The Finalizer removes it immediately, so the account's real ownership list is never persisted.
///
/// The target is the state machine's MoveNext, not the UpdateView stub. IL2CPP may inline the stub
/// into its callers: the 2026-10-19 build inlines it into AdventurerDetail.SubScene.OnRefreshAsync and
/// calls MoveNext directly, which drops UpdateView's caller count from 4 to 3 and makes a patch on the
/// stub unreachable on the page-refresh path. Every caller, inlined or not, still runs MoveNext.
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
        // Fail closed on field drift: the Prefix reads both of these by name.
        if (AccessTools.Property(typeof(UpdateViewStateMachine), "tCharacterId") == null ||
            AccessTools.Property(typeof(UpdateViewStateMachine), "__4__this") == null ||
            AccessTools.Property(typeof(UpdateViewStateMachine), "__1__state") == null)
        {
            throw new MissingMemberException(
                "AdventurerDetail UpdateView state machine no longer exposes tCharacterId/<>4__this/__1__state.");
        }

        return AccessTools.Method(
            typeof(UpdateViewStateMachine),
            nameof(UpdateViewStateMachine.MoveNext),
            Type.EmptyTypes);
    }

    internal static void Prefix(
        UpdateViewStateMachine __instance,
        out InjectionState __state)
    {
        __state = default;

        if (!PluginConfig.EnableUnownedCharacterDetail.Value || __instance == null)
        {
            return;
        }

        // State -1 is the initial synchronous segment; that segment contains the throwing lookup.
        // Later segments resume only to store the already resolved CharacterData.
        if (__instance.__1__state != -1)
        {
            return;
        }

        var detail = __instance.__4__this;
        if (detail == null)
        {
            return;
        }

        var tCharacterId = __instance.tCharacterId;
        var userData = detail._userData;
        if (PluginConfig.EnableLocalCharacterSkinChange.Value)
        {
            LocalCharacterSkinRegistry.ApplySavedSelection(
                userData,
                tCharacterId,
                detail._masterDataStore);
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
