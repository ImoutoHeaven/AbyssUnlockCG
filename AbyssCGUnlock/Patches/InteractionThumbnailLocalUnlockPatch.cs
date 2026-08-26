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
/// Redirects Interaction-page thumbnails for account-scoped synthetic unowned characters to the
/// downloaded Master cache. The native user-data loader enumerates its captured owned-character
/// list with FirstSafe, so CharacterDataStore.GetByTableId patches cannot service this lookup. A
/// saved local selection is routed by exact skin id rather than the character's default skin type.
/// </summary>
internal static class InteractionThumbnailLocalUnlockPatch
{
    private static bool _loggedFirstRedirect;
    private static bool _loggedFirstExactFallback;

    internal static MethodBase TargetMethod()
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

    internal static bool Prefix(
        CharacterThumbnailLoader __instance,
        AppDefine.IconSize iconSize,
        long mCharacterId,
        SkinType skinType,
        CacheType cacheType,
        ref UniTask<Sprite> __result)
    {
        if (!PluginConfig.EnableUnownedCharacterDetail.Value || __instance == null)
        {
            return true;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            if (userData == null ||
                !LocalCharacterRegistry.TryGet(userData, mCharacterId, out var character) ||
                character == null)
            {
                // Owned characters and IDs outside the current account snapshot remain native.
                return true;
            }

            var exactSkinId = 0L;
            try
            {
                var selection = LocalCharacterSkinRegistry.TryGet(
                    userData,
                    mCharacterId,
                    out var savedSelection)
                    ? savedSelection
                    : new CharacterSkinSelection(
                        character._BattleMCharacterSkinId_k__BackingField,
                        character._TavernMCharacterSkinId_k__BackingField);
                exactSkinId = CharacterThumbnailSelectionPolicy.ResolveExactSkinId(
                    selection,
                    skinType == SkinType.TavernWork);
                __result = exactSkinId > 0
                    ? __instance.LoadThumbnailBySkinIdAsync(iconSize, exactSkinId, cacheType)
                    : __instance.LoadThumbnailFromMasterDataAsync(
                        iconSize,
                        mCharacterId,
                        skinType,
                        cacheType);
            }
            catch (Exception exception)
            {
                if (!_loggedFirstExactFallback)
                {
                    _loggedFirstExactFallback = true;
                    CgUnlockPlugin.LogSource.LogWarning(
                        $"[CGUnlock] 未持有角色交流精确皮肤缩略图改道失败，回退Master默认路径: id={mCharacterId}, skin_id={exactSkinId}, error={exception}");
                }

                __result = __instance.LoadThumbnailFromMasterDataAsync(
                    iconSize,
                    mCharacterId,
                    skinType,
                    cacheType);
                exactSkinId = 0;
            }

            if (!_loggedFirstRedirect)
            {
                _loggedFirstRedirect = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 未持有角色交流缩略图已切换到精确皮肤路径: first_id={mCharacterId}, skin_type={skinType}, skin_id={exactSkinId}");
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
}
