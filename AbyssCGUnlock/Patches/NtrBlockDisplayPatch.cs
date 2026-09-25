using System.Reflection;
using HarmonyLib;
using Project.Interaction.AdventurerDetail;
using Project.Master;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// 寝室 CG 本地展示解锁：在原生缩略图首次渲染前写入最终解锁状态，
/// 并在渲染后清理通用羁绊遮罩及可选的 NTR 屏蔽块。
/// 目标：Project.Interaction.AdventurerDetail.StoryListPictThumbnail.UpdateView(StoryListThumbnailModel)
/// 证据：UpdateView 中 IsNtr && NTR屏蔽 时激活 _ntrBlockObject 并写入 NtrBlockHiddenText。
/// </summary>
internal static class NtrBlockDisplayPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(StoryListPictThumbnail),
            nameof(StoryListPictThumbnail.UpdateView),
            new[] { typeof(StoryListThumbnailModel) });
    }

    internal static void Prefix(StoryListThumbnailModel model)
    {
        if (model == null)
        {
            return;
        }

        var isSkinStory = model.NovelType == NovelType.CharacterSkin;
        model._IsExistStory_k__BackingField = UnlockPolicy.IsBedroomStoryUnlocked(
            model._IsExistStory_k__BackingField,
            isSkinStory,
            PluginConfig.EnableCharacterStoryUnlock.Value,
            PluginConfig.EnableSkinStoryUnlock.Value);
    }

    internal static void Postfix(StoryListPictThumbnail __instance, StoryListThumbnailModel model)
    {
        if (__instance == null || model == null)
        {
            return;
        }

        if (model._IsExistStory_k__BackingField)
        {
            var cover = __instance._coverObject;
            if (cover != null)
            {
                cover.SetActive(false);
            }

            var coverText = __instance._coverText;
            if (coverText != null)
            {
                coverText.gameObject.SetActive(false);
            }
        }

        if (PluginConfig.IgnoreNtrBlock.Value && model._IsNtr_k__BackingField)
        {
            var block = __instance._ntrBlockObject;
            if (block != null)
            {
                block.SetActive(false);
            }

            CgUnlockPlugin.LogSource.LogDebug($"[CGUnlock] NTR屏蔽块已隐藏: id={model.MNovelID}");
        }
    }
}
