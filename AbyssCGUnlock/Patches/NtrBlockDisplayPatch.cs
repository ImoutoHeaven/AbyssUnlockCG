using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Project.Interaction.AdventurerDetail;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// 无视 NTR 屏蔽：NTR 型卧室 CG 不再显示屏蔽块与隐藏文本。
/// 目标：Project.Interaction.AdventurerDetail.StoryListPictThumbnail.UpdateView(StoryListThumbnailModel)
/// 证据：UpdateView 中 IsNtr && NTR屏蔽 时激活 _ntrBlockObject 并写入 NtrBlockHiddenText。
/// </summary>
internal static class NtrBlockDisplayPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(StoryListPictThumbnail), nameof(StoryListPictThumbnail.UpdateView));
    }

    internal static void Postfix(StoryListPictThumbnail __instance, StoryListThumbnailModel model)
    {
        if (!PluginConfig.IgnoreNtrBlock.Value || __instance == null || model == null)
        {
            return;
        }

        if (!model._IsNtr_k__BackingField)
        {
            return;
        }

        var block = __instance._ntrBlockObject;
        if (block != null)
        {
            block.SetActive(false);
        }

        var coverText = __instance._coverText;
        if (coverText != null)
        {
            coverText.gameObject.SetActive(false);
        }

        // 已解锁模型不应留遮罩
        if (model._IsExistStory_k__BackingField)
        {
            var cover = __instance._coverObject;
            if (cover != null)
            {
                cover.SetActive(false);
            }
        }

        CgUnlockPlugin.LogSource.LogDebug($"[CGUnlock] NTR屏蔽块已隐藏: id={model.MNovelID}");
    }
}
