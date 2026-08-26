using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

public class LocalCharacterSkinSelectionTests
{
    [Theory]
    [InlineData(false, 130003503, 130003502, 130003503)]
    [InlineData(true, 130003503, 130003502, 130003502)]
    [InlineData(false, 0, 130003502, 0)]
    [InlineData(true, 130003503, 0, 0)]
    public void 未持有角色缩略图_按展示类型精确选择已缓存皮肤ID(
        bool isTavern,
        long battleSkinId,
        long tavernSkinId,
        long expectedSkinId)
    {
        var selection = new CharacterSkinSelection(battleSkinId, tavernSkinId);

        var actual = CharacterThumbnailSelectionPolicy.ResolveExactSkinId(
            selection,
            isTavern);

        Assert.Equal(expectedSkinId, actual);
    }

    [Theory]
    [InlineData("master_default", "session_paid", "session_paid", true)]
    [InlineData("master_default", "", "master_default", false)]
    public void 未持有详情Master只读模型_立绘与SD共同采用本地普通皮肤身份(
        string masterAssetId,
        string projectedAssetId,
        string expectedAssetId,
        bool expectedProjectedCharacterModel)
    {
        var plan = CharacterDetailPresentationPolicy.ResolveMasterReadOnly(
            masterAssetId,
            projectedAssetId);

        Assert.Equal(expectedAssetId, plan.DetailAssetId);
        Assert.Equal(expectedAssetId, plan.CharacterModelAssetId);
        Assert.Equal(expectedProjectedCharacterModel, plan.UseProjectedCharacterData);
    }

    [Fact]
    public void 皮肤磁盘缓存_重建存储实例后仍恢复且账号角色互相隔离()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "skin-selections.cache");

        try
        {
            var expected = new CharacterSkinSelection(130003503, 130003502);
            var writer = new CharacterSkinSelectionDiskCache(path);

            Assert.True(writer.Save(10001, 1300035, expected));

            var reopened = new CharacterSkinSelectionDiskCache(path);
            Assert.True(reopened.TryGet(10001, 1300035, out var restored));
            Assert.Equal(expected, restored);
            Assert.False(reopened.TryGet(10002, 1300035, out _));
            Assert.False(reopened.TryGet(10001, 1300018, out _));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void 皮肤磁盘缓存_损坏内容不阻断新的本地选择持久化()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "skin-selections.cache");

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "not-a-valid-cache");

            var cache = new CharacterSkinSelectionDiskCache(path);
            var expected = new CharacterSkinSelection(130001803, 130001802);

            Assert.False(cache.TryGet(20001, 1300018, out _));
            Assert.True(cache.Save(20001, 1300018, expected));

            var reopened = new CharacterSkinSelectionDiskCache(path);
            Assert.True(reopened.TryGet(20001, 1300018, out var restored));
            Assert.Equal(expected, restored);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void 已确认换装_同账号同角色重新打开时恢复会话选择()
    {
        var selections = new AccountScopedCharacterSkinSelections<string>();
        var expected = new CharacterSkinSelection(1102, 1203);

        selections.Save("account-a", 101, expected);

        Assert.True(selections.TryGet("account-a", 101, out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 换账号与换角色_选择互相隔离_切回原账号仍保留原值()
    {
        var selections = new AccountScopedCharacterSkinSelections<string>();
        var first = new CharacterSkinSelection(2101, 2201);
        var second = new CharacterSkinSelection(3101, 3201);
        var third = new CharacterSkinSelection(4101, 4201);

        selections.Save("account-a", 201, first);
        selections.Save("account-b", 201, second);
        selections.Save("account-a", 202, third);

        Assert.True(selections.TryGet("account-a", 201, out var restoredFirst));
        Assert.Equal(first, restoredFirst);
        Assert.True(selections.TryGet("account-b", 201, out var restoredSecond));
        Assert.Equal(second, restoredSecond);
        Assert.True(selections.TryGet("account-a", 202, out var restoredThird));
        Assert.Equal(third, restoredThird);
        Assert.False(selections.TryGet("account-b", 202, out _));
    }

    [Fact]
    public void 同账号同角色再次确认_只覆盖该角色的旧选择()
    {
        var selections = new AccountScopedCharacterSkinSelections<long>();
        var untouched = new CharacterSkinSelection(5101, 5201);
        var replacement = new CharacterSkinSelection(6102, 6202);

        selections.Save(77, 301, new CharacterSkinSelection(5100, 5200));
        selections.Save(77, 302, untouched);
        selections.Save(77, 301, replacement);

        Assert.True(selections.TryGet(77, 301, out var replaced));
        Assert.Equal(replacement, replaced);
        Assert.True(selections.TryGet(77, 302, out var retained));
        Assert.Equal(untouched, retained);
    }

    [Fact]
    public void 动态皮肤补齐_仅返回当前Master候选中尚未持有的唯一ID()
    {
        const long futureSkinA = 9_223_372_036_854_770_001L;
        const long futureSkinB = 9_223_372_036_854_770_002L;

        var planned = DynamicCharacterSkinEntitlementPlanner.Plan(
            new[] { futureSkinA, futureSkinB, futureSkinB, 0L },
            new[] { futureSkinA, 999L });

        Assert.Equal(new[] { futureSkinB }, planned);
    }

    [Fact]
    public void 本地确认_仅接受当前动态列表实际包含的普通与工作皮肤()
    {
        var current = new CharacterSkinSelection(7101, 7201);

        var accepted = CharacterSkinSelectionPolicy.Resolve(
            current,
            new CharacterSkinSelection(7102, 7202),
            new[] { 7101L, 7102L },
            new[] { 7201L, 7202L });
        var rejected = CharacterSkinSelectionPolicy.Resolve(
            current,
            new CharacterSkinSelection(7999, 7202),
            new[] { 7101L, 7102L },
            new[] { 7201L, 7202L });

        Assert.Equal(new CharacterSkinSelection(7102, 7202), accepted);
        Assert.Equal(new CharacterSkinSelection(7101, 7202), rejected);
    }

    [Theory]
    [InlineData(false, false, 8101, 8201, 8101, 8201, false)]
    [InlineData(false, false, 8101, 8201, 8102, 8201, true)]
    [InlineData(false, false, 8101, 8201, 8101, 8202, true)]
    [InlineData(true, false, 8101, 8201, 8101, 8201, true)]
    [InlineData(false, true, 8101, 8201, 8101, 8201, true)]
    public void 本地确认决策_动态角色与仅本地授权皮肤总是拦截_普通已持有皮肤保持原生行为(
        bool isSynthetic,
        bool requiresLocalEntitlement,
        long currentBattle,
        long currentTavern,
        long selectedBattle,
        long selectedTavern,
        bool expected)
    {
        var shouldHandle = CharacterSkinSelectionPolicy.ShouldHandleLocally(
            isSynthetic,
            new CharacterSkinSelection(currentBattle, currentTavern),
            new CharacterSkinSelection(selectedBattle, selectedTavern),
            requiresLocalEntitlement);

        Assert.Equal(expected, shouldHandle);
    }

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, false)]
    [InlineData(false, 0, false)]
    public void 换装弹窗完整性_仅动态未持有角色的空武器列表需要本地回退(
        bool isSynthetic,
        int weaponSkinCellCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            SkinChangePopupCompletenessPolicy.NeedsSyntheticWeaponFallback(
                isSynthetic,
                weaponSkinCellCount));
    }

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, false, true)]
    public void 皮肤授权判定_默认皮肤保持原生_未持有非默认与未知皮肤留在本地(
        bool isOwned,
        bool masterRowExists,
        bool isDefaultSkin,
        bool expected)
    {
        Assert.Equal(
            expected,
            CharacterSkinEntitlementPolicy.RequiresLocalEntitlement(
                isOwned,
                masterRowExists,
                isDefaultSkin));
    }

    [Fact]
    public void 动态解锁登记_临时用户授权移除后仍能按账号角色识别本地皮肤()
    {
        var entitlements = new AccountScopedCharacterSkinEntitlements<string>();

        entitlements.Register("account-a", 1300023, new[] { 130002302L, 130002303L });

        Assert.True(entitlements.Contains("account-a", 1300023, 130002302));
        Assert.True(entitlements.Contains("account-a", 1300023, 130002303));
        Assert.False(entitlements.Contains("account-b", 1300023, 130002302));
        Assert.False(entitlements.Contains("account-a", 1300035, 130002302));
    }

    [Fact]
    public void 弹窗当前皮肤缺失_从当前分类Master选择显示顺序最前的默认皮肤()
    {
        var resolved = CharacterSkinModelSelectionPolicy.ResolveCurrentSkinId(
            currentSkinId: 9999,
            new[]
            {
                (SkinId: 130003503L, IsDefault: false, DisplayOrder: 1),
                (SkinId: 130003502L, IsDefault: true, DisplayOrder: 20),
                (SkinId: 130003501L, IsDefault: true, DisplayOrder: 10),
            });

        Assert.Equal(130003501, resolved);
    }

    [Theory]
    [InlineData(1300018, 130001803, "chara_1300018_default", "chara_1300018_paid")]
    [InlineData(1300035, 130003503, "chara_1300035_default", "chara_1300035_paid")]
    public void 普通皮肤本地确认_将所选Battle皮肤的资源身份投影到角色展示模型(
        long characterId,
        long battleSkinId,
        string currentAssetId,
        string expectedAssetId)
    {
        var candidates = new[]
        {
            new CharacterSkinPresentationCandidate(
                skinId: 130001801,
                characterId: 1300018,
                skinType: 1,
                assetId: "chara_1300018_default"),
            new CharacterSkinPresentationCandidate(
                skinId: 130001803,
                characterId: 1300018,
                skinType: 1,
                assetId: "chara_1300018_paid"),
            new CharacterSkinPresentationCandidate(
                skinId: 130001802,
                characterId: 1300018,
                skinType: 2,
                assetId: "chara_1300018_tavern"),
            new CharacterSkinPresentationCandidate(
                skinId: 130003501,
                characterId: 1300035,
                skinType: 1,
                assetId: "chara_1300035_default"),
            new CharacterSkinPresentationCandidate(
                skinId: 130003503,
                characterId: 1300035,
                skinType: 1,
                assetId: "chara_1300035_paid")
        };

        var projected = CharacterSkinPresentationPolicy.ResolveBattleAssetId(
            characterId,
            battleSkinId,
            currentAssetId,
            candidates);

        Assert.Equal(expectedAssetId, projected);
    }

    [Fact]
    public void 展示资源投影_不会把工作皮肤或其他角色的资源写入普通立绘()
    {
        var candidates = new[]
        {
            new CharacterSkinPresentationCandidate(130001803, 1300999, 1, "other_character"),
            new CharacterSkinPresentationCandidate(130001803, 1300018, 2, "tavern_asset")
        };

        var projected = CharacterSkinPresentationPolicy.ResolveBattleAssetId(
            characterId: 1300018,
            battleSkinId: 130001803,
            currentAssetId: "current_asset",
            candidates);

        Assert.Equal("current_asset", projected);
    }
}
