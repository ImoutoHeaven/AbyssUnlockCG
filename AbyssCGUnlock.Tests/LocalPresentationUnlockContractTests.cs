using System.Text.RegularExpressions;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// 未持有角色只读技能展示、普通个人剧情与个人资料演出的本地解锁契约。
/// 这些补丁只能改用下载 Master 建模或改写现有 UI 模型，不得进入 API/async 状态机。
/// </summary>
public class LocalPresentationUnlockContractTests
{
    private static string Read(string relativePath) => File.ReadAllText(RepoRoot.SourceFile(relativePath));

    [Fact]
    public void 未持有角色技能_从账号技能模型切换到下载Master模型并保持只读()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterAbilityLocalViewPatch.cs");

        Assert.Contains("typeof(CharacterDetailModel)", src);
        Assert.Contains("nameof(CharacterDetailModel.CreateFromUserData)", src);
        Assert.Contains("CharacterDetailModel.CreateFromMaster(", src);
        Assert.Contains("SkillDescriptionModel.CreateModel", ProxyTargetsFixture.Text);
        Assert.Contains("AbilityModel\tLoadThumbnail", ProxyTargetsFixture.Text);
        Assert.Contains("AbilityModel\tGetDescription()", ProxyTargetsFixture.Text);
        Assert.Contains("character.ParameterData", src);
        Assert.Contains("LocalCharacterRegistry.TryGet", src);
        Assert.Contains("IL2CPP.Il2CppObjectBaseToPtr", src);

        Assert.Contains("typeof(CharacterAbilityUpInfoBoxViewService)", src);
        Assert.Contains("nameof(CharacterAbilityUpInfoBoxViewService.UpdateView)", src);
        Assert.Contains("_abilityUpButton", src);
        Assert.Contains("_resetButton", src);
        Assert.Contains("_abilityUpInformationViewList", src);
        Assert.Contains("_unlockButton", src);
        Assert.Contains("_fluctuationButtonGroup", src);
        Assert.Contains("_plusButton", src);
        Assert.Contains("_minusButton", src);
        Assert.Contains("_maxButton", src);
        Assert.Contains("enabled = false", src);
        Assert.Contains("interactable = false", src);
        Assert.Contains("DisabledButtonPointers", src);
        Assert.Contains("DisabledButtonPointers.Remove", src);
        Assert.Contains("PrefixRestoreTrackedButtons", src);
        Assert.Contains("Invalidate the previous character's ownership before native UpdateView", src);
        Assert.Contains("OwnedSkillViewPointers.Remove(viewPointer)", src);
        Assert.Contains("BlockedSkillViewPointers.Add(viewPointer)", src);
        Assert.Contains("OwnedSkillViewPointers.Clear()", src);
        Assert.Contains("ApplyAllMutationButtonStatesBestEffort", src);
        Assert.Contains("TargetAbilityUpCommandMethod", src);
        Assert.Contains("TargetUnlockCommandMethod", src);
        Assert.Contains("PrefixAbilityUpCommand", src);
        Assert.Contains("PrefixUnlockCommand", src);
        Assert.Contains("out bool __state", src);
        Assert.Contains("PostfixModelFactory", src);
        Assert.Contains("BlockedSkillModelPointers", src);
        Assert.Contains("BlockedSkillViewPointers", src);
        Assert.Contains("OwnedSkillViewPointers.Contains", src);
        Assert.Contains("unknown views both fail closed", src);
        Assert.Contains("按未知来源阻断", src);
        Assert.DoesNotContain("保持原生流程", src);
        Assert.DoesNotContain("TargetMethod(typeof(CharacterAbilityUpApiDataStore)", src);
        Assert.DoesNotContain("MoveNext", src);
        Assert.DoesNotContain("UniTask<", src);
    }

    [Fact]
    public void 已持有角色技能_真实账号行优先且按钮状态交回原生UpdateView重算()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterAbilityLocalViewPatch.cs");
        var manager = Read("AbyssCGUnlock/Patches/PatchManager.cs");

        Assert.Contains("IsExactOwnedCharacterRow", src);
        Assert.Contains("IsCharacterIdOwnedByCurrentAccount", src);
        Assert.Contains("Registry identity wins over _dataList membership", src);
        Assert.True(
            src.IndexOf("LocalCharacterRegistry.TryGet(userData, character.MCharaId", StringComparison.Ordinal) <
            src.IndexOf("IsExactOwnedCharacterRow(userData, character)", StringComparison.Ordinal));
        Assert.Contains("IL2CPP.Il2CppObjectBaseToPtr(row) == characterPointer", src);
        Assert.Contains("IL2CPP.Il2CppObjectBaseToPtr(row) != registryPointer", src);
        Assert.Contains("__state = false", src);
        Assert.Contains("OwnedSkillModelPointers.Add", src);
        Assert.Contains("row.MCharaId == mCharacterId", src);
        Assert.Contains("PrefixRestoreTrackedButtons", src);
        Assert.Contains("before native UpdateView runs", src);
        Assert.Contains("successful postfix may promote", src);
        Assert.Contains("prefix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PrefixRestoreTrackedButtons))", manager);
        Assert.Contains("postfix: new HarmonyMethod(typeof(CharacterAbilityLocalViewPatch), nameof(CharacterAbilityLocalViewPatch.PostfixModelFactory))", manager);
    }

    [Fact]
    public void 普通个人剧情_在缩略图首次渲染前仅改写现有模型的解锁位()
    {
        var src = Read("AbyssCGUnlock/Patches/PersonalStoryUnlockPatch.cs");

        Assert.Contains("typeof(StoryListThumbnail)", src);
        Assert.Contains("nameof(StoryListThumbnail.UpdateView)", src);
        Assert.Contains("StoryListThumbnailModel model", src);
        Assert.Contains("!PluginConfig.EnableNovelReadBypass.Value", src);
        Assert.Contains("_IsExistStory_k__BackingField = true", src);
        Assert.Contains("普通 StoryListViewController 使用 StoryListThumbnail", ProxyTargetsFixture.Text);
        Assert.DoesNotContain("StoryListPictThumbnail", src);
        Assert.DoesNotContain("R18StoryListViewController", src);
        Assert.DoesNotContain("ref int", src);
        Assert.DoesNotContain("MoveNext", src);
        Assert.DoesNotContain("ValueType", src);
    }

    [Fact]
    public void 个人资料演出_只解锁eventList并在原生列表渲染前完成()
    {
        var src = Read("AbyssCGUnlock/Patches/ProfileReplayUnlockPatch.cs");

        Assert.Contains("typeof(ProfileReplayViewController)", src);
        Assert.Contains("nameof(ProfileReplayViewController.UpdateView)", src);
        Assert.Contains("ProfileReplayModelList = Il2CppSystem.Collections.Generic.List", src);
        Assert.Contains("Prefix(ProfileReplayModelList eventList)", src);
        Assert.Contains("model.IsLocked = false", src);
        Assert.DoesNotContain("voiceList[", src);
        Assert.DoesNotContain("MoveNext", src);
        Assert.DoesNotContain("ValueType", src);
    }

    [Fact]
    public void 三项本地展示补丁_不得调用API请求或网络上报原语()
    {
        var src = string.Join(Environment.NewLine,
            Read("AbyssCGUnlock/Patches/CharacterAbilityLocalViewPatch.cs"),
            Read("AbyssCGUnlock/Patches/PersonalStoryUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/ProfileReplayUnlockPatch.cs"));

        Assert.DoesNotMatch(new Regex(@"\b\w*ApiDataStore\b", RegexOptions.CultureInvariant), src);
        Assert.DoesNotMatch(new Regex(@"\.\s*Request\w*\s*\(", RegexOptions.CultureInvariant), src);
        Assert.DoesNotMatch(new Regex(@"\b(?:HttpClient|HttpWebRequest|WebRequest|UnityWebRequest|UnityUserReportingPlatform)\b", RegexOptions.CultureInvariant), src);
        Assert.DoesNotContain("System.Net", src);
        Assert.DoesNotContain("SendWebRequest", src);
    }

    [Fact]
    public void 普通个人剧情_只在空奖励本地阅读拦截启用时暴露锁定入口()
    {
        var story = Read("AbyssCGUnlock/Patches/PersonalStoryUnlockPatch.cs");
        var novelRead = Read("AbyssCGUnlock/Patches/NovelReadBypassPatch.cs");

        Assert.Contains("!PluginConfig.EnableNovelReadBypass.Value", story);
        Assert.Contains("novelType != NovelType.Character && novelType != NovelType.CharacterSkin", novelRead);
        Assert.Contains("fake.read_rewards = new Il2CppReferenceArray<DropContentEntity>(0)", novelRead);
        Assert.Contains("return false", novelRead);
    }

    [Fact]
    public void 反编译夹具_覆盖普通剧情与演出的构造来源和点击分支()
    {
        Assert.Contains("intimacyLevel >= condition_value -> StoryListThumbnailModel.IsExistStory", ProxyTargetsFixture.Text);
        Assert.Contains("UpdateView eventList -> this._eventList -> CreateList", ProxyTargetsFixture.Text);
        Assert.Contains("IsLocked=false -> OpenNovelPopup/Prize/Cast", ProxyTargetsFixture.Text);
        Assert.Contains("IsLocked=true -> OpenHint", ProxyTargetsFixture.Text);
        Assert.Contains("ProfileReplayViewController contains no ApiDataStore.RequestAsync", ProxyTargetsFixture.Text);
    }

    [Fact]
    public void 已持有角色_普通个人剧情和个人资料演出也绕过羁绊门槛()
    {
        var story = Read("AbyssCGUnlock/Patches/PersonalStoryUnlockPatch.cs");
        var replay = Read("AbyssCGUnlock/Patches/ProfileReplayUnlockPatch.cs");

        Assert.Contains("both owned and registry-backed unowned", story);
        Assert.Contains("both owned and", replay);
        Assert.DoesNotContain("LocalCharacterRegistry", story);
        Assert.DoesNotContain("LocalCharacterRegistry", replay);
        Assert.DoesNotContain("EnableUnownedCharacterDetail", story);
        Assert.DoesNotContain("EnableUnownedCharacterDetail", replay);
    }

    [Fact]
    public void 配置和注册_默认包含三个新增本地展示绕过()
    {
        var config = Read("AbyssCGUnlock/PluginConfig.cs");
        var manager = Read("AbyssCGUnlock/Patches/PatchManager.cs");

        Assert.Contains("EnableUnownedCharacterSkillView", config);
        Assert.Contains("EnablePersonalStoryUnlock", config);
        Assert.Contains("EnableProfileReplayUnlock", config);

        Assert.Contains("CharacterAbilityLocalViewPatch", manager);
        Assert.Contains("PersonalStoryUnlockPatch", manager);
        Assert.Contains("ProfileReplayUnlockPatch", manager);
    }

    [Theory]
    [InlineData("CharacterDetailModel\tCreateFromUserData(Project.Master.MasterDataStore,Project.User.CharacterData)")]
    [InlineData("CharacterDetailModel\tCreateFromMaster(Project.Master.MasterDataStore,System.Int64,System.Int32,System.Int32,System.Boolean)")]
    [InlineData("CharacterAbilityUpInfoBoxViewService\tUpdateView(Project.Common.CharacterDetailModel)")]
    [InlineData("CharacterAbilityUpInfoBoxViewService+__c\t_InitializeViewAsync_b__15_1(UniRx.Unit,Project.CharacterDetail.CharacterAbilityUpInfoBoxViewService) -> System.Void")]
    [InlineData("CharacterAbilityUpInfoBoxViewService+__c__DisplayClass33_0\t_OpenUnlockConfirmPopupAsync_b__0() -> System.Void")]
    [InlineData("StoryListThumbnail\tUpdateView(Project.Interaction.AdventurerDetail.StoryListThumbnailModel,System.Int32)")]
    [InlineData("ProfileReplayViewController\tUpdateView(Il2CppSystem.Collections.Generic.List<ProfileReplayListModel>,Il2CppSystem.Collections.Generic.List<ProfileReplayListModel>,Project.User.CharacterData)")]
    public void 新补丁目标_必须存在于新鲜代理证据夹具(string signature)
    {
        Assert.Contains(signature, ProxyTargetsFixture.Text);
    }
}
