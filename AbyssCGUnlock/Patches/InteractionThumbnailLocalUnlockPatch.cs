#nullable enable

using System;
using System.Reflection;
using Absf;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Project;
using Project.Master;
using Project.ThumbnailLoader;
using Project.User;
using UnityEngine;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Routes a saved local selection by exact skin id at the loader boundary, before either the
/// user-data or Master default request can start. The user-data fallback still redirects synthetic
/// unowned characters to downloaded Master data because its native FirstSafe lookup only contains
/// account-owned rows.
/// </summary>
internal static class InteractionThumbnailLocalUnlockPatch
{
    private static bool _loggedFirstRedirect;
    private static bool _loggedFirstExactRedirect;
    private static bool _loggedFirstExactFailure;

    internal static MethodBase TargetUserDataMethod()
    {
        return AccessTools.Method(
            typeof(CharacterThumbnailLoader),
            nameof(CharacterThumbnailLoader.LoadThumbnailFromUserDataAsync),
            new[]
            {
                typeof(AppDefine.IconSize),
                typeof(long),
                typeof(SkinType),
                typeof(CacheType),
            });
    }

    internal static MethodBase TargetMasterDataMethod()
    {
        return AccessTools.Method(
            typeof(CharacterThumbnailLoader),
            nameof(CharacterThumbnailLoader.LoadThumbnailFromMasterDataAsync),
            new[]
            {
                typeof(AppDefine.IconSize),
                typeof(long),
                typeof(SkinType),
                typeof(CacheType),
            });
    }

    internal static bool PrefixUserData(
        CharacterThumbnailLoader __instance,
        AppDefine.IconSize iconSize,
        long mCharacterId,
        SkinType skinType,
        CacheType cacheType,
        ref UniTask<Sprite> __result)
    {
        if (__instance == null)
        {
            return true;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            if (TryRouteSavedSelection(
                    __instance,
                    userData,
                    iconSize,
                    mCharacterId,
                    skinType,
                    cacheType,
                    ref __result))
            {
                return false;
            }

            if (!PluginConfig.EnableUnownedCharacterDetail.Value ||
                userData == null ||
                !LocalCharacterRegistry.TryGet(userData, mCharacterId, out var character) ||
                character == null)
            {
                // Owned characters and IDs outside the current account snapshot remain native.
                return true;
            }

            __result = __instance.LoadThumbnailFromMasterDataAsync(
                iconSize,
                mCharacterId,
                skinType,
                cacheType);

            if (!_loggedFirstRedirect)
            {
                _loggedFirstRedirect = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 未持有角色交流缩略图已切换到Master路径: first_id={mCharacterId}, skin_type={skinType}");
            }

            return false;
        }
        catch (Exception exception)
        {
            CgUnlockPlugin.LogSource.LogWarning(
                $"[CGUnlock] 未持有角色交流缩略图Master改道失败，回退原生路径: id={mCharacterId}, error={exception}");
            return true;
        }
    }

    internal static bool PrefixMasterData(
        CharacterThumbnailLoader __instance,
        AppDefine.IconSize iconSize,
        long mCharacterId,
        SkinType skinType,
        CacheType cacheType,
        ref UniTask<Sprite> __result)
    {
        if (__instance == null)
        {
            return true;
        }

        try
        {
            return !TryRouteSavedSelection(
                __instance,
                Engine.Get<UserData>(),
                iconSize,
                mCharacterId,
                skinType,
                cacheType,
                ref __result);
        }
        catch (Exception exception)
        {
            LogExactFailureOnce(mCharacterId, 0, exception);
            return true;
        }
    }

    private static bool TryRouteSavedSelection(
        CharacterThumbnailLoader loader,
        UserData? userData,
        AppDefine.IconSize iconSize,
        long mCharacterId,
        SkinType skinType,
        CacheType cacheType,
        ref UniTask<Sprite> result)
    {
        var hasSavedSelection = LocalCharacterSkinRegistry.TryGet(
            userData,
            mCharacterId,
            out var savedSelection);
        var plan = CharacterThumbnailFirstRenderPolicy.Resolve(
            PluginConfig.EnableLocalCharacterSkinChange.Value,
            hasSavedSelection,
            savedSelection,
            skinType == SkinType.TavernWork);
        if (!plan.UseExactSkinId)
        {
            return false;
        }

        try
        {
            result = loader.LoadThumbnailBySkinIdAsync(iconSize, plan.SkinId, cacheType);
        }
        catch (Exception exception)
        {
            LogExactFailureOnce(mCharacterId, plan.SkinId, exception);
            return false;
        }

        if (!_loggedFirstExactRedirect)
        {
            _loggedFirstExactRedirect = true;
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 角色首屏缩略图已直接采用本地精确皮肤: first_id={mCharacterId}, " +
                $"skin_type={skinType}, skin_id={plan.SkinId}, default_request=false");
        }

        return true;
    }

    private static void LogExactFailureOnce(
        long mCharacterId,
        long skinId,
        Exception exception)
    {
        if (_loggedFirstExactFailure)
        {
            return;
        }

        _loggedFirstExactFailure = true;
        CgUnlockPlugin.LogSource.LogWarning(
            $"[CGUnlock] 本地精确皮肤缩略图改道失败，回退原生路径: " +
            $"id={mCharacterId}, skin_id={skinId}, error={exception}");
    }
}
