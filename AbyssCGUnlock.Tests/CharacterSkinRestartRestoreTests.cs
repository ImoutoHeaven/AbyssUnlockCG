using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

public class CharacterSkinRestartRestoreTests
{
    [Fact]
    public void 游戏重启后_第一次角色列表缩略图请求直接使用磁盘皮肤()
    {
        var plan = CharacterThumbnailFirstRenderPolicy.Resolve(
            localSkinChangeEnabled: true,
            hasSavedSelection: true,
            new CharacterSkinSelection(1_300_035_03, 1_300_035_02),
            isTavern: false);

        Assert.Equal((true, 1_300_035_03L), (plan.UseExactSkinId, plan.SkinId));
    }

    [Fact]
    public void 游戏重启后_磁盘选择在首个展示模型创建前覆盖默认皮肤身份()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "skin-selections.cache");

        try
        {
            const long accountId = 922_714_267;
            const long characterId = 1_300_035;
            var persisted = new CharacterSkinSelection(1_300_035_03, 1_300_035_02);
            var writer = new CharacterSkinSelectionDiskCache(path);
            Assert.True(writer.Save(accountId, characterId, persisted));

            var restartedCache = new CharacterSkinSelectionDiskCache(path);
            Assert.True(restartedCache.TryGet(accountId, characterId, out var restored));

            var plan = CharacterSkinRestartRestorePolicy.Resolve(
                characterId,
                new CharacterSkinSelection(1_300_035_01, 1_300_035_02),
                "chara_1300035_default",
                restored,
                new[]
                {
                    new CharacterSkinPresentationCandidate(
                        1_300_035_01,
                        characterId,
                        1,
                        "chara_1300035_default"),
                    new CharacterSkinPresentationCandidate(
                        1_300_035_03,
                        characterId,
                        1,
                        "chara_1300035_paid"),
                    new CharacterSkinPresentationCandidate(
                        1_300_035_02,
                        characterId,
                        2,
                        "chara_1300035_tavern")
                });

            Assert.Equal(
                (1_300_035_03L, 1_300_035_02L, "chara_1300035_paid"),
                (plan.Selection.BattleSkinId, plan.Selection.TavernSkinId, plan.AssetId));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
