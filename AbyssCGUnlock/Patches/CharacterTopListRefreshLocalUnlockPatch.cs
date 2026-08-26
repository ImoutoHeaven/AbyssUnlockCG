#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Absf;
using HarmonyLib;
using Project.CharacterTop;
using Project.User;
using Il2CppCharacterDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterData>;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Covers every native CharacterTop list rebuild, including later sort/filter changes. The
/// controller retains its CharacterData array, but model creation still resolves IDs from the
/// current CharacterDataStore, so registry entries exist only for this synchronous call.
/// </summary>
internal static class CharacterTopListRefreshLocalUnlockPatch
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
            typeof(CharacterListWithFavoriteViewController),
            nameof(CharacterListWithFavoriteViewController.RefreshActiveCharacterList),
            Type.EmptyTypes);
    }

    internal static void Prefix(out InjectionState? __state)
    {
        __state = null;

        if (!PluginConfig.EnableUnownedCharacterDetail.Value)
        {
            return;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            if (userData == null)
            {
                return;
            }

            var list = userData._CharaDataStore_k__BackingField?._dataList;
            if (list == null)
            {
                return;
            }

            var snapshot = LocalCharacterRegistry.Snapshot(userData);
            if (snapshot.Count == 0)
            {
                return;
            }

            var state = new InjectionState(list);
            __state = state;

            for (var i = 0; i < snapshot.Count; i++)
            {
                var character = snapshot[i];
                if (character == null || Contains(list, character._Id_k__BackingField))
                {
                    continue;
                }

                list.Add(character);
                state.Added.Add(character);
            }
        }
        catch (Exception exception)
        {
            RemoveInjected(__state);
            __state = null;
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 队伍角色列表重建注入失败，已回退原生列表: {exception}");
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
