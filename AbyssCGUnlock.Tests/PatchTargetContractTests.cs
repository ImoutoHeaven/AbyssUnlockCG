using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// 补丁绑定的源码契约测试（镜像 Abyss-AutoNether 的 InteropContractTests 风格）：
/// 断言补丁代码中的目标类型/方法/字段名与新鲜反编译证据（代理程序集元数据）完全一致。
/// 回归断言：不得再使用代理改写前的原始闭包标识符（真机崩溃根因，见 docs/rca-crash-*.md）。
/// 证据：evidence/fixtures/proxy-targets.tsv（由 evidence/dump/types.tsv + members.tsv 提取）。
/// </summary>
public class PatchTargetContractTests
{
    private static string Read(string relativePath) => File.ReadAllText(RepoRoot.SourceFile(relativePath));

    [Fact]
    public void 解锁补丁_目标为公开方法UpdateListView_与代理元数据一致()
    {
        var src = Read("AbyssCGUnlock/Patches/StoryListUnlockPatch.cs");
        Assert.Contains("R18StoryListViewController", src);
        Assert.Contains("nameof(R18StoryListViewController.UpdateListView)", src);
        Assert.Contains("ActiveModels", src);
        Assert.Contains("NovelType.CharacterSkin", src);
        Assert.Contains("_IsExistStory_k__BackingField", src);
    }

    [Fact]
    public void 回归_源码不得使用代理改写前的原始闭包标识符()
    {
        var patchesDir = RepoRoot.SourceFile("AbyssCGUnlock/Patches");
        var all = string.Join(Environment.NewLine, Directory.EnumerateFiles(patchesDir, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        // 真机崩溃根因：GetNestedType("<>c__DisplayClass11_0") 在代理程序集里是 __c__DisplayClass11_0
        Assert.DoesNotContain("<>c__DisplayClass11_0", all);
        Assert.DoesNotContain("<UpdateListView>b__1", all);
        Assert.DoesNotContain("<UpdateListView>b__0", all);
    }

    [Fact]
    public void 回归_不得用NonPublic标志查找代理类型_代理类型均为public()
    {
        var patchesDir = RepoRoot.SourceFile("AbyssCGUnlock/Patches");
        var all = string.Join(Environment.NewLine, Directory.EnumerateFiles(patchesDir, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.DoesNotContain("BindingFlags.NonPublic", all);
    }

    [Fact]
    public void NTR屏蔽补丁_目标类型与方法名_与代理元数据一致()
    {
        var src = Read("AbyssCGUnlock/Patches/NtrBlockDisplayPatch.cs");
        Assert.Contains("StoryListPictThumbnail", src);
        Assert.Contains("nameof(StoryListPictThumbnail.UpdateView)", src);
    }

    [Fact]
    public void NTR屏蔽补丁_使用证据中的视图对象字段()
    {
        var src = Read("AbyssCGUnlock/Patches/NtrBlockDisplayPatch.cs");
        Assert.Contains("_ntrBlockObject", src);
        Assert.Contains("_coverText", src);
        Assert.Contains("_coverObject", src);
        Assert.Contains("_IsNtr_k__BackingField", src);
    }

    [Fact]
    public void 配置项_默认开启四个绕过_符合用户决策()
    {
        var src = Read("AbyssCGUnlock/PluginConfig.cs");
        Assert.Contains("EnableSkinStoryUnlock", src);
        Assert.Contains("EnableCharacterStoryUnlock", src);
        Assert.Contains("IgnoreNtrBlock", src);
        Assert.Contains("EnableNovelReadBypass", src);
    }

    [Fact]
    public void 补丁注册_包含解锁与NTR屏蔽与NovelRead伪造()
    {
        var src = Read("AbyssCGUnlock/Patches/PatchManager.cs");
        Assert.Contains("StoryListUnlockPatch", src);
        Assert.Contains("NtrBlockDisplayPatch", src);
        Assert.Contains("NovelReadBypassPatch", src);
    }

    [Fact]
    public void NovelRead伪造补丁_目标与方法名_与代理元数据一致()
    {
        var src = Read("AbyssCGUnlock/Patches/NovelReadBypassPatch.cs");
        Assert.Contains("Project.Api.NovelApiDataStore", src);
        Assert.Contains("nameof(NovelApiDataStore.RequestAsync)", src);
        Assert.Contains("NovelReadResponseEntity", src);
        Assert.Contains("UniTask.FromResult", src);
        Assert.Contains("read_rewards", src);
        Assert.Contains("NovelType.CharacterSkin", src);
        Assert.Contains("NovelType.Character", src);
    }
}
