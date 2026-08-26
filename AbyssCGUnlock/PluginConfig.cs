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

    /// <summary>允许游戏当前动态列表中的未持有角色进入角色详情及本地场景页。</summary>
    internal static ConfigEntry<bool> EnableUnownedCharacterDetail = null!;

    /// <summary>绕过工作用服装入口的本地酒馆登记（低好感度）遮罩。</summary>
    internal static ConfigEntry<bool> EnableNtrSceneEntryBypass = null!;

    /// <summary>未持有角色技能从下载 Master 只读渲染，升级/重置入口保持禁用。</summary>
    internal static ConfigEntry<bool> EnableUnownedCharacterSkillView = null!;

    /// <summary>绕过三个普通个人剧情的羁绊等级门槛。</summary>
    internal static ConfigEntry<bool> EnablePersonalStoryUnlock = null!;

    /// <summary>绕过个人资料→其他→演出的本地前提条件。</summary>
    internal static ConfigEntry<bool> EnableProfileReplayUnlock = null!;

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

        EnableUnownedCharacterDetail = plugin.Config.Bind(
            "CharacterDetail",
            nameof(EnableUnownedCharacterDetail),
            true,
            "允许进入游戏当前动态未持有角色列表中的角色详情，并只在 UpdateView 同步解析期间临时注入本地 CharacterData；不修改突破或服务端持有状态。");

        EnableNtrSceneEntryBypass = plugin.Config.Bind(
            "CharacterDetail",
            nameof(EnableNtrSceneEntryBypass),
            true,
            "绕过角色详情→交流→工作用服装的本地酒馆登记遮罩；只在原生切换回调执行期间临时置位并立即恢复，不发送请求。");

        EnableUnownedCharacterSkillView = plugin.Config.Bind(
            "CharacterDetail",
            nameof(EnableUnownedCharacterSkillView),
            true,
            "未持有角色的技能/能力改用下载 Master 数据渲染图标与说明；升级、解锁、加减级和重置按钮全部禁用，不发送请求。");

        EnablePersonalStoryUnlock = plugin.Config.Bind(
            "Interaction",
            nameof(EnablePersonalStoryUnlock),
            true,
            "本地解锁交流页的三个普通个人剧情，无视羁绊 1/5/15 门槛；只改写 StoryListThumbnailModel 的客户端显示门控。为保证纯本地播放，仅在 EnableNovelReadBypass 同时启用时生效。");

        EnableProfileReplayUnlock = plugin.Config.Bind(
            "Interaction",
            nameof(EnableProfileReplayUnlock),
            true,
            "本地解锁个人资料→其他→演出列表，无视羁绊及其他前提；只改写传给 ProfileReplayViewController 的 eventList 模型。");
    }
}
