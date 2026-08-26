#nullable enable

using System;
using System.Reflection;
using Absf;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Project.User;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Supplies a registry-backed local CharacterData before the active account store attempts its
/// throwing FirstSafe lookup. Only IDs in the current account's dynamic unowned snapshot skip the
/// original method. The target has a simple (instance, Int64) native signature, avoiding the
/// pointer-backed CancellationToken/Nullable arguments on CharacterDetail's update method.
/// </summary>
internal static class CharacterDataStoreLocalLookupPatch
{
    private static bool _loggedFirstLocalLookup;

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CharacterDataStore),
            nameof(CharacterDataStore.GetByTableId),
            new[] { typeof(long) });
    }

    internal static bool Prefix(
        CharacterDataStore __instance,
        long tId,
        ref CharacterData? __result)
    {
        if (!PluginConfig.EnableUnownedCharacterDetail.Value ||
            __instance == null)
        {
            return true;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            var activeStore = userData?._CharaDataStore_k__BackingField;
            if (userData == null ||
                activeStore == null ||
                IL2CPP.Il2CppObjectBaseToPtr(activeStore) != IL2CPP.Il2CppObjectBaseToPtr(__instance) ||
                !LocalCharacterRegistry.TryGet(userData, tId, out var character) ||
                character == null)
            {
                // Owned characters, stale IDs and stores outside the active account stay native.
                return true;
            }

            character._IsNew_k__BackingField = false;
            character._IsExistStory_k__BackingField = true;
            __result = character;

            if (!_loggedFirstLocalLookup)
            {
                _loggedFirstLocalLookup = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] CharacterDataStoreLocalLookup 命中账号绑定未持有角色: first_id={tId}");
            }

            // GetByTableId uses FirstSafe and throws for unowned IDs, so an after-hook cannot recover.
            // Returning false here is the only branch that skips the native lookup.
            return false;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色本地查询失败，回退原生路径: id={tId}, error={exception}");
            return true;
        }
    }
}
