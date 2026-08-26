using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace AbyssCGUnlock;

/// <summary>BepInEx 配置项（首次加载后生成于 BepInEx/config/AbyssCGUnlock.cfg）。</summary>
internal static class PluginConfig
{
    /// <summary>绕过皮肤购买检查（普通型与 NTR 型卧室 CG 共用同一判定）。</summary>
    internal static ConfigEntry<bool> EnableSkinStoryUnlock = null!;

    /// <summary>绕过好感度门槛（MNovelCharacters.condition_value 检查）。</summary>
    internal static ConfigEntry<bool> EnableCharacterStoryUnlock = null!;

    /// <summary>无视 NTR 屏蔽（NTR 场景不再显示屏蔽块/隐藏文本）。</summary>
    internal static ConfigEntry<bool> IgnoreNtrBlock = null!;

    /// <summary>本地伪造 NovelRead API 成功响应，跳过服务端校验（190103 好感度不足 / 190108 未购皮肤）。</summary>
    internal static ConfigEntry<bool> EnableNovelReadBypass = null!;

    internal static void Initialize(BasePlugin plugin)
    {
        EnableSkinStoryUnlock = plugin.Config.Bind(
            "CGUnlock",
            nameof(EnableSkinStoryUnlock),
            true,
            "绕过「需拥有对应皮肤才能查看卧室 CG」的检查（补丁点：R18StoryListViewController.UpdateListView 的 NovelType=CharacterSkin 模型，证据 evidence/fixtures/proxy-targets.tsv）。");

        EnableCharacterStoryUnlock = plugin.Config.Bind(
            "CGUnlock",
            nameof(EnableCharacterStoryUnlock),
            true,
            "绕过好感度门槛 intimacyLevel >= condition_value（补丁点：UpdateListView 的非 CharacterSkin 模型）。");

        IgnoreNtrBlock = plugin.Config.Bind(
            "CGUnlock",
            nameof(IgnoreNtrBlock),
            true,
            "无视 NTR 屏蔽设置，NTR 型卧室 CG 不再被遮挡（证据：StoryListPictThumbnail.UpdateView 的 ntrBlockObject 分支）。");

        EnableNovelReadBypass = plugin.Config.Bind(
            "CGUnlock",
            nameof(EnableNovelReadBypass),
            true,
            "本地伪造 NovelRead API 成功响应（仅 Character/CharacterSkin 类型），跳过服务端校验错误 190103（好感度不足）/190108（未购皮肤）。证据：ConfirmNovelFlashbackPopupController...b__16_0 → NovelApiDataStore.RequestAsync；ApiErrorMessageBuilder.FormatServerErrorMessage。");
    }
}
