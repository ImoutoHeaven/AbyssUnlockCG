using System.Reflection;
using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// RED：动态角色选择策略只依赖当次 Master 开放状态与当前账号持有集合。
/// 通过反射加载待实现类型，让缺失实现表现为测试失败而非测试工程编译失败。
/// </summary>
public class DynamicUnownedCharacterSelectorTests
{
    private static IReadOnlyList<long> Select(
        IEnumerable<KeyValuePair<long, bool>> candidates,
        ISet<long> ownedMasterIds)
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
                typeof(IEnumerable<KeyValuePair<long, bool>>),
                typeof(ISet<long>)
            },
            modifiers: null)!;

        return (IReadOnlyList<long>)method.Invoke(null, new object[] { candidates, ownedMasterIds })!;
    }

    [Fact]
    public void 只选择已开放且当前账号未持有角色_并稳定去重()
    {
        const long futureCharacterId = 9_223_372_036_854_770_001L;
        var candidates = new[]
        {
            new KeyValuePair<long, bool>(101, true),
            new KeyValuePair<long, bool>(202, true),
            new KeyValuePair<long, bool>(303, false),
            new KeyValuePair<long, bool>(futureCharacterId, true),
            new KeyValuePair<long, bool>(101, true)
        };

        var selected = Select(candidates, new HashSet<long> { 202 });

        Assert.Equal(new[] { 101L, futureCharacterId }, selected);
    }

    [Fact]
    public void 换账号时使用新的持有集合重新计算_不复用账号专属结果()
    {
        var candidates = new[]
        {
            new KeyValuePair<long, bool>(11, true),
            new KeyValuePair<long, bool>(22, true)
        };

        Assert.Equal(new[] { 22L }, Select(candidates, new HashSet<long> { 11 }));
        Assert.Equal(new[] { 11L }, Select(candidates, new HashSet<long> { 22 }));
    }
}
