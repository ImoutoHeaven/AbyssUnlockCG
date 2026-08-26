#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Absf;
using HarmonyLib;
using Project.User;
using CharacterTopRefreshStateMachine = Project.CharacterTop.SubScene._OnRefreshAsync_d__10;
using Il2CppCharacterDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterData>;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Temporarily extends Team -> Characters in the real native refresh call site. IL2CPP inlines
/// SubService.UpdateView into the initial MoveNext of SubScene.OnRefreshAsync, so patching the
/// standalone SubService method cannot observe this page.
/// </summary>
internal static class CharacterTopLocalUnlockPatch
{
    internal sealed class InjectionState
    {
        internal InjectionState(Il2CppCharacterDataList list)
        {
            List = list;
            Added = new List<CharacterData>();
        }

        internal Il2CppCharacterDataList List { get; }
        internal List<CharacterData> Added { get; }
    }

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CharacterTopRefreshStateMachine),
            nameof(CharacterTopRefreshStateMachine.MoveNext),
            Type.EmptyTypes);
    }

    internal static void Prefix(
        CharacterTopRefreshStateMachine __instance,
        out InjectionState? __state)
    {
        __state = null;
        UserData? userData = null;

        // State -1 is the initial synchronous MoveNext. State 0 resumes after the base refresh await.
        if (__instance == null || __instance.__1__state != -1)
        {
            return;
        }

        try
        {
            userData = Engine.Get<UserData>();
            if (userData == null)
            {
                return;
            }

            if (!PluginConfig.EnableUnownedCharacterDetail.Value)
            {
                LocalCharacterRegistry.Clear(userData);
                return;
            }

            var list = userData._CharaDataStore_k__BackingField?._dataList;
            if (list == null)
            {
                LocalCharacterRegistry.Clear(userData);
                return;
            }

            var syntheticCharacters = DynamicCharacterCatalog.Refresh(userData);
            var state = new InjectionState(list);
            __state = state;

            for (var i = 0; i < syntheticCharacters.Count; i++)
            {
                var character = syntheticCharacters[i];
                if (character == null || Contains(list, character._Id_k__BackingField))
                {
                    continue;
                }

                list.Add(character);
                state.Added.Add(character);
            }

            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 队伍角色页已本地追加动态未持有角色: discovered={syntheticCharacters.Count}, injected={state.Added.Count}");
        }
        catch (Exception exception)
        {
            RemoveInjected(__state);
            __state = null;
            if (userData != null)
            {
                LocalCharacterRegistry.Clear(userData);
            }

            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 队伍角色页动态角色注入失败，已回退原生列表: {exception}");
        }
    }

    internal static Exception? Finalizer(Exception? __exception, InjectionState? __state)
    {
        RemoveInjected(__state);
        return __exception;
    }

    private static bool Contains(Il2CppCharacterDataList list, long characterId)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var current = list[i];
            if (current != null && current._Id_k__BackingField == characterId)
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveInjected(InjectionState? state)
    {
        if (state == null || state.List == null)
        {
            return;
        }

        for (var i = state.Added.Count - 1; i >= 0; i--)
        {
            state.List.Remove(state.Added[i]);
        }

        state.Added.Clear();
    }
}
