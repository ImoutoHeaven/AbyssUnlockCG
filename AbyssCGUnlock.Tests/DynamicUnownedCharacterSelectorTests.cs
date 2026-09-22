using System.Reflection;
using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// 选择不看客户端公开列表。资源门单独决定能否合成。
/// 通过反射加载待实现类型，让缺失实现表现为测试失败而非测试工程编译失败。
/// </summary>
public class DynamicUnownedCharacterSelectorTests
{
    private static IReadOnlyList<long> Select(IEnumerable<long> candidateIds, ISet<long> ownedMasterIds)
    {
        var type = typeof(UnlockPolicy).Assembly.GetType(
            "AbyssCGUnlock.DynamicUnownedCharacterSelector",
            throwOnError: true)!;
        var method = type.GetMethod(
            "Select",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(IEnumerable<long>),
                typeof(ISet<long>)
            },
            modifiers: null)!;

        return (IReadOnlyList<long>)method.Invoke(null, new object[] { candidateIds, ownedMasterIds })!;
    }

    [Fact]
    public void 选择当前账号未持有角色_包含未开放并稳定去重()
    {
        const long futureCharacterId = 9_223_372_036_854_770_001L;
        var candidateIds = new long[] { 101, 202, 303, futureCharacterId, 101 };

        var selected = Select(candidateIds, new HashSet<long> { 202 });

        Assert.Equal(new[] { 101L, 303L, futureCharacterId }, selected);
    }

    [Fact]
    public void 换账号时使用新的持有集合重新计算_不复用账号专属结果()
    {
        var candidateIds = new long[] { 11, 22 };

        Assert.Equal(new[] { 22L }, Select(candidateIds, new HashSet<long> { 11 }));
        Assert.Equal(new[] { 11L }, Select(candidateIds, new HashSet<long> { 22 }));
    }

    [Theory]
    [InlineData(true, true, true, true, true)]
    [InlineData(false, true, true, true, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, false, true, false)]
    [InlineData(true, true, true, false, false)]
    public void 缺工会_默认皮肤资源_档案或酒馆卡片时不合成(
        bool hasUnionType,
        bool hasDefaultBattleSkinAsset,
        bool hasProfile,
        bool hasTavernWorkSkin,
        bool expected)
    {
        Assert.Equal(
            expected,
            DynamicUnownedCharacterSelector.CanLocallyUnlock(
                hasUnionType,
                hasDefaultBattleSkinAsset,
                hasProfile,
                hasTavernWorkSkin));
    }
}

