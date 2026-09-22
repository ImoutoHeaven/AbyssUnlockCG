using System.Reflection;
using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// RED：合成未持有角色必须使用 Tavern 卡片实际引用的默认工作皮肤，
/// 不能把普通 type=1 默认皮肤误当成 TavernMCharacterSkinId。
/// </summary>
public class DynamicTavernCharacterSkinSelectorTests
{
    private static IReadOnlyDictionary<long, long> Select(
        IEnumerable<(long SkinId, long CharacterId, int Type, int IsDefault)> skins,
        IEnumerable<(long CharacterId, long SkinId)> tavernCardPairs)
    {
        var type = typeof(UnlockPolicy).Assembly.GetType(
            "AbyssCGUnlock.DynamicTavernCharacterSkinSelector",
            throwOnError: true)!;
        var method = type.GetMethod(
            "Select",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(IEnumerable<(long SkinId, long CharacterId, int Type, int IsDefault)>),
                typeof(IEnumerable<(long CharacterId, long SkinId)>),
            },
            modifiers: null)!;

        return (IReadOnlyDictionary<long, long>)method.Invoke(
            null,
            new object[] { skins, tavernCardPairs })!;
    }

    [Fact]
    public void 只选择被Tavern卡片引用的type2默认工作皮肤_同角色稳定取首项()
    {
        var selected = Select(
            new[]
            {
                (SkinId: 1001L, CharacterId: 101L, Type: 1, IsDefault: 1),
                (SkinId: 1002L, CharacterId: 101L, Type: 2, IsDefault: 1),
                (SkinId: 1003L, CharacterId: 101L, Type: 2, IsDefault: 2),
                (SkinId: 1004L, CharacterId: 101L, Type: 2, IsDefault: 1),
                (SkinId: 2001L, CharacterId: 202L, Type: 2, IsDefault: 1),
                (SkinId: 3001L, CharacterId: 303L, Type: 2, IsDefault: 1),
            },
            new[]
            {
                (CharacterId: 101L, SkinId: 1001L),
                (CharacterId: 101L, SkinId: 1002L),
                (CharacterId: 101L, SkinId: 1003L),
                (CharacterId: 101L, SkinId: 1004L),
                (CharacterId: 303L, SkinId: 3999L),
            });

        Assert.Single(selected);
        Assert.Equal(1002L, selected[101L]);
        Assert.False(selected.ContainsKey(202L));
        Assert.False(selected.ContainsKey(303L));
    }

    [Fact]
    public void 支持未来long角色与皮肤ID_不依赖预编码范围()
    {
        const long futureCharacterId = 9_223_372_036_854_770_001L;
        const long futureSkinId = 9_223_372_036_854_770_002L;

        var selected = Select(
            new[]
            {
                (SkinId: futureSkinId, CharacterId: futureCharacterId, Type: 2, IsDefault: 1),
            },
            new[]
            {
                (CharacterId: futureCharacterId, SkinId: futureSkinId),
            });

        Assert.Equal(futureSkinId, selected[futureCharacterId]);
    }

    [Fact]
    public void 没有type2默认皮肤时使用卡片实际引用且存在的皮肤()
    {
        var selected = Select(
            new[]
            {
                (SkinId: 1001L, CharacterId: 101L, Type: 1, IsDefault: 1),
                (SkinId: 2001L, CharacterId: 202L, Type: 2, IsDefault: 0),
            },
            new[]
            {
                (CharacterId: 101L, SkinId: 1001L),
                (CharacterId: 202L, SkinId: 2999L),
            });

        Assert.Equal(1001L, selected[101L]);
        Assert.False(selected.ContainsKey(202L));
    }
}
