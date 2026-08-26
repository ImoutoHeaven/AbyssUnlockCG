using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// 补丁目标解析回路（Phase 1 反馈环）：
/// 用真实代理程序集元数据夹具校验补丁用到的每一个标识符都逐字存在于游戏 Project.dll 代理程序集中。
/// 夹具：evidence/fixtures/proxy-targets.tsv（2026-08-26 由 evidence/dump/types.tsv + members.tsv 提取）。
/// </summary>
public class ProxyResolutionFixtureTests
{
    private static string Fixture => ProxyTargetsFixture.Text;
    private static string Patches =>
        string.Join(Environment.NewLine, Directory.EnumerateFiles(RepoRoot.SourceFile("AbyssCGUnlock/Patches"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

    [Fact]
    public void 夹具记录了代理名称被改写的证据()
    {
        // 原始闭包名 <>c__DisplayClass11_0 在代理程序集里是 __c__DisplayClass11_0
        Assert.Contains("R18StoryListViewController+__c__DisplayClass11_0", Fixture);
        Assert.Contains("_UpdateListView_b__0", Fixture);
        Assert.Contains("_UpdateListView_b__1", Fixture);
    }

    [Theory]
    [InlineData("R18StoryListViewController")]
    [InlineData("UpdateListView")]
    [InlineData("ActiveModels")]
    [InlineData("NovelType")]
    [InlineData("CharacterSkin")]
    [InlineData("StoryListPictThumbnail")]
    [InlineData("UpdateView")]
    [InlineData("_IsExistStory_k__BackingField")]
    [InlineData("_ntrBlockObject")]
    [InlineData("_coverText")]
    [InlineData("_coverObject")]
    [InlineData("_IsNtr_k__BackingField")]
    [InlineData("NovelApiDataStore")]
    [InlineData("RequestAsync")]
    [InlineData("NovelReadResponseEntity")]
    [InlineData("UniTask")]
    [InlineData("read_rewards")]
    public void 补丁使用的标识符必须存在于真实代理元数据夹具(string identifier)
    {
        Assert.Contains(identifier, Fixture);
        Assert.Contains(identifier, Patches);
    }

    [Fact]
    public void 服务端校验证据_错误码展示层与请求已记录()
    {
        Assert.Contains("NovelApiDataStore.RequestAsync", Fixture);
        Assert.Contains("Public_Static", Fixture);
        Assert.Contains("190108", Fixture);
        Assert.Contains("190103", Fixture);
        // 展示层证据（原文 ISIL ApiErrorMessageBuilder.txt:554，另见 docs/rca-error-190103-190108.md）
        Assert.Contains("エラーが発生しました", Fixture);
    }

    [Fact]
    public void NovelType赋值证据_Character为3_CharacterSkin为5()
    {
        // 枚举声明顺序：None,Main,Event,Character,Home,CharacterSkin,Prologue,Other
        Assert.Contains("ISIL StoryListThumbnailModel.txt:235 Move NovelType,3 (MNovelCharacters 构造器)", Fixture);
        Assert.Contains("ISIL StoryListThumbnailModel.txt:423 Move NovelType,5 (MNovelCharacterSkins 构造器)", Fixture);
        var charIdx = Fixture.IndexOf("Character : Project.Master.NovelType", StringComparison.Ordinal);
        var skinIdx = Fixture.IndexOf("CharacterSkin : Project.Master.NovelType", StringComparison.Ordinal);
        Assert.True(charIdx >= 0 && skinIdx > charIdx);
    }
}
