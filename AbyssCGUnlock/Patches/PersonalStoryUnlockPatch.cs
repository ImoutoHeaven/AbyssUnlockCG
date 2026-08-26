using System.Reflection;
using HarmonyLib;
using Project.Interaction.AdventurerDetail;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Unlocks the three ordinary character stories for both owned and registry-backed unowned
/// characters before their thumbnails first render. The model was already built from dynamic
/// MNovelCharacters Master rows; only its client-side bond gate is changed. Playback continues
/// through the existing local NovelRead bypass for Character stories.
/// </summary>
internal static class PersonalStoryUnlockPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(StoryListThumbnail),
            nameof(StoryListThumbnail.UpdateView),
            new[] { typeof(StoryListThumbnailModel), typeof(int) });
    }

    internal static void Prefix(StoryListThumbnailModel model)
    {
        if (!PluginConfig.EnablePersonalStoryUnlock.Value ||
            !PluginConfig.EnableNovelReadBypass.Value ||
            model == null)
        {
            return;
        }

        var wasLocked = !model._IsExistStory_k__BackingField;
        model._IsExistStory_k__BackingField = true;

        if (wasLocked)
        {
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 角色个人剧情已强制本地解锁(无视羁绊门槛): id={model.MNovelID}, script={model.MNovelScriptId}");
        }
    }
}
