using System;
using System.Reflection;
using HarmonyLib;
using Project.User;
using DetailSubService = Project.Interaction.AdventurerDetail.SubService;
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
///
/// The state machine type name is compiler-generated and renumbered by every game update
/// (&lt;UpdateView&gt;d__101 became d__104 on 2026-10-30), so it is resolved by method name at precheck
/// and its members are read through cached reflection handles.
/// </summary>
internal static class CharacterDetailLocalUnlockPatch
{
    private static PropertyInfo _stateProperty = null!;
    private static PropertyInfo _characterIdProperty = null!;
    private static PropertyInfo _serviceProperty = null!;

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
        var stateMachine = GeneratedTargets.FindStateMachine(
            typeof(DetailSubService),
            nameof(DetailSubService.UpdateView));

        // Fail closed on field drift: the Prefix reads all three by name.
        _stateProperty = AccessTools.Property(stateMachine, "__1__state");
        _characterIdProperty = AccessTools.Property(stateMachine, "tCharacterId");
        _serviceProperty = AccessTools.Property(stateMachine, "__4__this");
        if (_stateProperty == null || _characterIdProperty == null || _serviceProperty == null)
        {
            throw new MissingMemberException(
                stateMachine.FullName + " no longer exposes tCharacterId/<>4__this/<>1__state.");
        }

        // The Prefix casts these reads, so the types are part of the contract and must fail at precheck,
        // not as an InvalidCastException inside the patched state machine.
        if (_stateProperty.PropertyType != typeof(int) ||
            _characterIdProperty.PropertyType != typeof(long) ||
            _serviceProperty.PropertyType != typeof(DetailSubService))
        {
            throw new MissingMemberException(
                stateMachine.FullName + " changed member types: expected <>1__state:int, tCharacterId:long, <>4__this:" +
                typeof(DetailSubService).FullName + ".");
        }

        var moveNext = AccessTools.Method(stateMachine, "MoveNext", Type.EmptyTypes);
        if (moveNext == null)
        {
            throw new MissingMethodException(stateMachine.FullName + ".MoveNext() is missing.");
        }

        return moveNext;
    }

    internal static void Prefix(
        object __instance,
        out InjectionState __state)
    {
        __state = default;

        if (!PluginConfig.EnableUnownedCharacterDetail.Value || __instance == null)
        {
            return;
        }

        // State -1 is the initial synchronous segment; that segment contains the throwing lookup.
        // Later segments resume only to store the already resolved CharacterData.
        if ((int)_stateProperty.GetValue(__instance)! != -1)
        {
            return;
        }

        var detail = (DetailSubService)_serviceProperty.GetValue(__instance)!;
        if (detail == null)
        {
            return;
        }

        var tCharacterId = (long)_characterIdProperty.GetValue(__instance)!;
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
