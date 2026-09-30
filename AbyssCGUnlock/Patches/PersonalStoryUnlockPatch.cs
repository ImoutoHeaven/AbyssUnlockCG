using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Project.Interaction.AdventurerDetail;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Unlocks the three ordinary character stories for both owned and registry-backed unowned
/// characters. The model was already built from dynamic MNovelCharacters Master rows; only its
/// client-side bond gate is changed. Playback continues through the existing local NovelRead
/// bypass for Character stories.
///
/// 目标（公开方法，名称与代理程序集逐字一致，证据 evidence/fixtures/proxy-targets.tsv）：
///   Project.Interaction.AdventurerDetail.StoryListViewController.OnContentViewActivateEvent(int, StoryListThumbnail)
///
/// 补丁点选择依据（原生 ISIL，2026-09-30 快照）：
///   交流页列表的锁定遮罩由 OnContentViewActivateEvent 内联决定：它读取 ActiveModels[index] 的
///   IsExistStory 字段（指令 187），随即 content._coverObject.SetActive(IsExistStory == 0)（指令 192），
///   并且不再调用 StoryListThumbnail.UpdateView（该方法全程序集调用方计数为 0）。
///   遮罩状态在内容激活时定下并在该内容存活期间保持，因此必须在同一调用内、读取之前改写门控字段：
///   prefix 先写 ActiveModels[index].IsExistStory = true，紧接的方法体便隐藏遮罩。
///   门控来源：StoryListThumbnailModel 的 isExistStory 由 intimacyLevel &gt;= condition_value 产生
///   （StoryListViewController.__c__DisplayClass9_0.&lt;UpdateListView&gt;b__0）。
///
/// 证据：ISIL StoryListViewController.txt（OnContentViewActivateEvent 指令 38/187/192/212）、
///       StoryListViewController_NestedType___c__DisplayClass9_0.txt、
///       evidence/fixtures/storylist-personal-unlock-evidence.txt。
/// </summary>
internal static class PersonalStoryUnlockPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(StoryListViewController),
            nameof(StoryListViewController.OnContentViewActivateEvent),
            new[] { typeof(int), typeof(StoryListThumbnail) });
    }

    internal static void Prefix(StoryListViewController __instance, int index)
    {
        if (!PluginConfig.EnablePersonalStoryUnlock.Value ||
            !PluginConfig.EnableNovelReadBypass.Value ||
            __instance == null)
        {
            return;
        }

        var models = __instance.ActiveModels;
        if (models == null)
        {
            return;
        }

        // IReadOnlyList 代理只暴露索引器；把同一指针包装成具体 List<T> 代理来遍历
        var list = new Il2CppSystem.Collections.Generic.List<StoryListThumbnailModel>(IL2CPP.Il2CppObjectBaseToPtr(models));
        if (index < 0 || index >= list.Count)
        {
            return;
        }

        var model = list[index];
        if (model == null)
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
