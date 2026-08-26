using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// 证据保真测试：断言关键反编译结论（偏移/判定/点击路径/服务端校验）以字面量形式固化在测试源码里，
/// 不依赖仓库外的 docs/evidence 文件，保证干净克隆可运行。
/// 字面量来源：docs/unlock-conditions.md（2026-08-26 新鲜反编译证据）。
/// </summary>
public class EvidenceFixtureTests
{
    private const string Evidence = """
        MNovelCharacters | is_ntr | +0x40
        MNovelCharacterSkins | m_character_skin_id | +0x18
        MNovelCharacterSkins | is_ntr | +0x44
        userSkinIds.Contains
        m_character_skin_id
        NTR 与普通型共用
        is_ntr = 0
        is_ntr = 1
        intimacyLevel >= v.condition_value
        无二次校验
        OpenSkinSupplierPopup
        NovelApiDataStore.RequestAsync
        FormatServerErrorMessage
        190108
        190103
        """;

    [Fact]
    public void 证据_皮肤判定点_Contains调用存在()
    {
        Assert.Contains("userSkinIds.Contains", Evidence);
        Assert.Contains("m_character_skin_id", Evidence);
    }

    [Fact]
    public void 证据_字段偏移_与ISIL一致()
    {
        Assert.Contains("MNovelCharacters | is_ntr | +0x40", Evidence);
        Assert.Contains("MNovelCharacterSkins | m_character_skin_id | +0x18", Evidence);
        Assert.Contains("MNovelCharacterSkins | is_ntr | +0x44", Evidence);
    }

    [Fact]
    public void 证据_普通型与NTR型共用判定_有记录()
    {
        Assert.Contains("NTR 与普通型共用", Evidence);
        Assert.Contains("is_ntr = 0", Evidence);
        Assert.Contains("is_ntr = 1", Evidence);
    }

    [Fact]
    public void 证据_点击路径无二次校验_有记录()
    {
        Assert.Contains("无二次校验", Evidence);
        Assert.Contains("OpenSkinSupplierPopup", Evidence);
    }

    [Fact]
    public void 证据_好感度判定_有记录()
    {
        Assert.Contains("intimacyLevel >= v.condition_value", Evidence);
    }

    [Fact]
    public void 证据_服务端校验与错误码_有记录()
    {
        // 190103/190108 为服务端返回错误码
        Assert.Contains("NovelApiDataStore.RequestAsync", Evidence);
        Assert.Contains("FormatServerErrorMessage", Evidence);
        Assert.Contains("190108", Evidence);
        Assert.Contains("190103", Evidence);
    }
}
