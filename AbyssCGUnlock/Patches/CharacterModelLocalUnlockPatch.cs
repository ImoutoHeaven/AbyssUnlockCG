#nullable enable

using System;
using System.Reflection;
using Absf;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Project;
using Project.Outgame;
using Project.ThumbnailLoader;
using Project.User;
using Il2CppCancellationToken = Il2CppSystem.Threading.CancellationToken;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Redirects only locally synthesized unowned characters away from the owned-user-data
/// thumbnail path. CharacterTop creates thumbnails lazily, after its temporary store injection
/// has already been removed; the master-data model keeps the same CharacterData while resolving
/// its thumbnail from downloaded master caches instead of the current account's owned list. When
/// a local skin selection exists, the lazy thumbnail is replaced with the exact skin-id loader.
/// </summary>
internal static class CharacterModelLocalUnlockPatch
{
    private static bool _loggedFirstRedirect;
    private static bool _loggedFirstExactFallback;

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
        var enableUnownedCharacterDetail = PluginConfig.EnableUnownedCharacterDetail.Value;
        var enableLocalCharacterSkinChange = PluginConfig.EnableLocalCharacterSkinChange.Value;
        if (!enableUnownedCharacterDetail && !enableLocalCharacterSkinChange)
        {
            return true;
        }

        try
        {
            var userData = Engine.Get<UserData>();
            if (userData == null)
            {
                return true;
            }

            if (enableLocalCharacterSkinChange)
            {
                LocalCharacterSkinRegistry.ApplySavedSelection(userData, tCharacterId);
            }

            if (!enableUnownedCharacterDetail ||
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

            var exactSkinId = 0L;
            var exactThumbnailApplied = false;
            try
            {
                var selection = LocalCharacterSkinRegistry.TryGet(
                    userData,
                    tCharacterId,
                    out var savedSelection)
                    ? savedSelection
                    : new CharacterSkinSelection(
                        character._BattleMCharacterSkinId_k__BackingField,
                        character._TavernMCharacterSkinId_k__BackingField);
                exactSkinId = CharacterThumbnailSelectionPolicy.ResolveExactSkinId(
                    selection,
                    isTavern: false);
                var thumbnailLoader = Engine.Get<IThumbnailLoaderService>()?.CharacterThumbnailLoader;
                if (exactSkinId > 0 && thumbnailLoader != null)
                {
                    var exactThumbnail = new LazyLoadThumbnail(
                        thumbnailLoader.LoadThumbnailBySkinIdAsync(
                            size,
                            exactSkinId,
                            CacheType.Scene),
                        ct);
                    __result._Thumbnail_k__BackingField =
                        exactThumbnail.Cast<ILazyLoadableThumbnail>();
                    exactThumbnailApplied = true;
                }
            }
            catch (Exception exception)
            {
                if (!_loggedFirstExactFallback)
                {
                    _loggedFirstExactFallback = true;
                    CgUnlockPlugin.LogSource.LogWarning(
                        $"[CGUnlock] 未持有角色精确皮肤缩略图投影失败，保留Master默认缩略图: id={tCharacterId}, skin_id={exactSkinId}, error={exception}");
                }
            }

            if (!_loggedFirstRedirect)
            {
                _loggedFirstRedirect = true;
                CgUnlockPlugin.LogSource.LogInfo(
                    $"[CGUnlock] 动态未持有角色模型已切换到Master只读路径: first_id={tCharacterId}, exact_skin_id={exactSkinId}, exact_thumbnail={exactThumbnailApplied}");
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
