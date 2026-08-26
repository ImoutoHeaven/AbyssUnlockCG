using System.Reflection;
using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// RED：本地角色目录必须绑定当前 UserData，并由游戏当次生成的列表整体替换。
/// 通过反射加载待实现类型，使 RED 表现为测试失败而不是测试工程编译失败。
/// </summary>
public class AccountScopedCharacterCatalogTests
{
    private static object CreateCatalog()
    {
        var definition = typeof(UnlockPolicy).Assembly.GetType(
            "AbyssCGUnlock.AccountScopedCharacterCatalog`1",
            throwOnError: true)!;
        return Activator.CreateInstance(definition.MakeGenericType(typeof(string)), nonPublic: true)!;
    }

    private static void Replace(object catalog, nint ownerKey, params KeyValuePair<long, string>[] entries)
    {
        catalog.GetType()
            .GetMethod("Replace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(catalog, new object?[] { ownerKey, entries });
    }

    private static bool TryGet(object catalog, nint ownerKey, long characterId, out string? value)
    {
        var arguments = new object?[] { ownerKey, characterId, null };
        var found = (bool)catalog.GetType()
            .GetMethod("TryGet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(catalog, arguments)!;
        value = (string?)arguments[2];
        return found;
    }

    private static IReadOnlyList<string> Snapshot(object catalog, nint ownerKey)
    {
        return (IReadOnlyList<string>)catalog.GetType()
            .GetMethod("Snapshot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(catalog, new object?[] { ownerKey })!;
    }

    [Fact]
    public void 同账号刷新_整体替换而非合并_旧角色立即失效()
    {
        var catalog = CreateCatalog();
        var account = (nint)0x101;

        Replace(catalog, account,
            new KeyValuePair<long, string>(11, "old"),
            new KeyValuePair<long, string>(22, "keep"));
        Replace(catalog, account,
            new KeyValuePair<long, string>(22, "refreshed"),
            new KeyValuePair<long, string>(33, "new"));

        Assert.False(TryGet(catalog, account, 11, out _));
        Assert.True(TryGet(catalog, account, 22, out var refreshed));
        Assert.Equal("refreshed", refreshed);
        Assert.True(TryGet(catalog, account, 33, out _));
    }

    [Fact]
    public void 换账号_旧账号快照不能命中_新账号快照独立生效()
    {
        var catalog = CreateCatalog();
        var firstAccount = (nint)0x201;
        var secondAccount = (nint)0x202;

        Replace(catalog, firstAccount, new KeyValuePair<long, string>(44, "first"));
        Assert.False(TryGet(catalog, secondAccount, 44, out _));

        Replace(catalog, secondAccount, new KeyValuePair<long, string>(55, "second"));

        Assert.False(TryGet(catalog, firstAccount, 44, out _));
        Assert.True(TryGet(catalog, secondAccount, 55, out var value));
        Assert.Equal("second", value);
    }

    [Fact]
    public void 任意未来角色ID_无需预编码即可由新快照发现()
    {
        var catalog = CreateCatalog();
        var account = (nint)0x301;
        const long futureCharacterId = 9_223_372_036_854_770_001L;

        Replace(catalog, account, new KeyValuePair<long, string>(futureCharacterId, "future"));

        Assert.True(TryGet(catalog, account, futureCharacterId, out var value));
        Assert.Equal("future", value);
    }

    [Fact]
    public void 空快照_清除同账号上一轮全部角色()
    {
        var catalog = CreateCatalog();
        var account = (nint)0x401;

        Replace(catalog, account, new KeyValuePair<long, string>(66, "stale"));
        Replace(catalog, account, Array.Empty<KeyValuePair<long, string>>());

        Assert.False(TryGet(catalog, account, 66, out _));
    }

    [Fact]
    public void 快照_只返回当前owner的当次值_换账号与替换后不会泄漏旧结果()
    {
        var catalog = CreateCatalog();
        var firstAccount = (nint)0x501;
        var secondAccount = (nint)0x502;

        Replace(catalog, firstAccount,
            new KeyValuePair<long, string>(71, "first-a"),
            new KeyValuePair<long, string>(72, "first-b"));

        Assert.Equal(new[] { "first-a", "first-b" }, Snapshot(catalog, firstAccount));
        Assert.Empty(Snapshot(catalog, secondAccount));

        Replace(catalog, secondAccount, new KeyValuePair<long, string>(81, "second"));

        Assert.Empty(Snapshot(catalog, firstAccount));
        Assert.Equal(new[] { "second" }, Snapshot(catalog, secondAccount));
    }
}
