using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Project.Interaction.AdventurerDetail;
using Project.Master;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// 卧室 CG 列表解锁补丁（真机崩溃修复版 v0.1.1）。
///
/// 目标（公开方法，名称与代理程序集逐字一致，证据 evidence/fixtures/proxy-targets.tsv）：
///   Project.Interaction.AdventurerDetail.R18StoryListViewController.UpdateListView(...)
///
/// 崩溃根因（RCA）：v0.1.0 试图补编译器闭包方法，但 Il2CppInterop 生成的代理程序集
/// 把原始名改写（&lt;&gt;c__DisplayClass11_0 → __c__DisplayClass11_0、
/// &lt;UpdateListView&gt;b__1 → _UpdateListView_b__1，且全部 public），
/// GetNestedType("&lt;&gt;c__DisplayClass11_0", NonPublic) 返回 null →
/// Harmony.Patch 抛 "Null method for com.abyss.cgunlock"。
///
/// 修复：改为 postfix 公开方法 UpdateListView，遍历 ActiveModels：
///   - NovelType == CharacterSkin（ISIL 证据值 5）→ 皮肤付费墙绕过；
///   - 其余（Character，ISIL 证据值 3）→ 好感度门槛绕过。
/// 证据：ISIL StoryListThumbnailModel.txt:235/423（构造器 NovelType 赋值）。
/// </summary>
internal static class StoryListUnlockPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(R18StoryListViewController), nameof(R18StoryListViewController.UpdateListView));
    }

    internal static void Postfix(R18StoryListViewController __instance)
    {
        if (__instance == null)
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
        for (var i = 0; i < list.Count; i++)
        {
            var model = list[i];
            if (model == null)
            {
                continue;
            }

            if (model.NovelType == NovelType.CharacterSkin)
            {
                if (PluginConfig.EnableSkinStoryUnlock.Value)
                {
                    model._IsExistStory_k__BackingField = true;
                    CgUnlockPlugin.LogSource.LogInfo($"[CGUnlock] 皮肤卧室CG已强制解锁: id={model.MNovelID} script={model.MNovelScriptId}");
                }
            }
            else
            {
                if (PluginConfig.EnableCharacterStoryUnlock.Value)
                {
                    model._IsExistStory_k__BackingField = true;
                    CgUnlockPlugin.LogSource.LogInfo($"[CGUnlock] 角色CG已强制解锁(无视好感度门槛): id={model.MNovelID} script={model.MNovelScriptId}");
                }
            }
        }
    }
}
