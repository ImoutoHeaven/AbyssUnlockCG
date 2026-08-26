using AbyssCGUnlock.Patches;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace AbyssCGUnlock;

[BepInPlugin(PluginInfo.PluginGuid, PluginInfo.PluginName, PluginInfo.PluginVersion)]
public class CgUnlockPlugin : BasePlugin
{
    internal static ManualLogSource LogSource = null!;

    public override void Load()
    {
        LogSource = Log;
        PluginConfig.Initialize(this);
        PatchManager.Initialize();

        Log.LogInfo($"[CGUnlock] 已加载。皮肤解锁绕过={PluginConfig.EnableSkinStoryUnlock.Value} / 好感度解锁绕过={PluginConfig.EnableCharacterStoryUnlock.Value} / 无视NTR屏蔽={PluginConfig.IgnoreNtrBlock.Value} / NovelRead伪造={PluginConfig.EnableNovelReadBypass.Value} / 未持有角色详情={PluginConfig.EnableUnownedCharacterDetail.Value} / NTR入口遮罩绕过={PluginConfig.EnableNtrSceneEntryBypass.Value} / 未持有技能只读展示={PluginConfig.EnableUnownedCharacterSkillView.Value} / 个人剧情解锁={PluginConfig.EnablePersonalStoryUnlock.Value} / 演出解锁={PluginConfig.EnableProfileReplayUnlock.Value} / 本地角色换装={PluginConfig.EnableLocalCharacterSkinChange.Value}");
    }
}

internal static class PluginInfo
{
    internal const string PluginGuid = "com.abyss.cgunlock";
    internal const string PluginName = "AbyssCGUnlock";
    internal const string PluginVersion = "0.3.16";
}
