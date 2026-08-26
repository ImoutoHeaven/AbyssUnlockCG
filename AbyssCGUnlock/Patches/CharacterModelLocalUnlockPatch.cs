#nullable enable

using System;
using System.Reflection;
using Absf;
using HarmonyLib;
using Project;
using Project.Outgame;
using Project.User;
using Il2CppCancellationToken = Il2CppSystem.Threading.CancellationToken;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Redirects only locally synthesized unowned characters away from the owned-user-data
/// thumbnail path. CharacterTop creates thumbnails lazily, after its temporary store injection
/// has already been removed; the master-data model keeps the same CharacterData while resolving
/// its thumbnail from downloaded master caches instead of the current account's owned list.
/// </summary>
internal static class CharacterModelLocalUnlockPatch
{
    private static bool _loggedFirstRedirect;

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CharacterModel),
            nameof(CharacterModel.CreateFromUser),
            new[]
            {
                typeof(long),
                typeof(AppDefine.IconSize),
                typeof(Il2CppCancellationToken),
            });
    }

    internal static bool Prefix(
        long tCharacterId,
        AppDefine.IconSize size,
        Il2CppCancellationToken ct,
        ref CharacterModel? __result)
    {
        if (!PluginConfig.EnableUnownedCharacterDetail.Value)
        {
            return true;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            if (userData == null ||
                !LocalCharacterRegistry.TryGet(userData, tCharacterId, out var character) ||
                character == null)
            {
                // Owned characters and IDs outside the current account snapshot remain native.
                return true;
            }

            __result = CharacterModel.CreateFromMaster(character, size, ct);
            if (__result == null)
            {
                CgUnlockPlugin.LogSource.LogWarning(
                    $"[CGUnlock] 动态未持有角色Master模型创建返回空，回退原生路径: id={tCharacterId}");
                return true;
            }

            if (!_loggedFirstRedirect)
            {
                _loggedFirstRedirect = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 动态未持有角色模型已切换到Master缩略图路径: first_id={tCharacterId}");
            }

            return false;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 动态未持有角色Master模型改道失败，回退原生路径: id={tCharacterId}, error={exception}");
            return true;
        }
    }
}
