using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// 红-绿循环 #1：解锁语义纯函数。
/// 预期值来自新鲜反编译证据（docs/unlock-conditions.md §3）：
/// - 皮肤卧室 CG：isExistStory = userSkinIds.Contains(m_character_skin_id)
/// - 角色 CG：isExistStory = intimacyLevel >= condition_value
/// </summary>
public class UnlockPolicyTests
{
    private static readonly long[] OwnedSkins = { 101L, 202L, 303L };

    // ---- 皮肤卧室 CG ----

    [Fact]
    public void 皮肤CG_游戏原语义_持有皮肤则解锁()
    {
        Assert.True(UnlockPolicy.IsSkinStoryUnlockedByGame(101, OwnedSkins));
    }

    [Fact]
    public void 皮肤CG_游戏原语义_未持有皮肤则锁定()
    {
        Assert.False(UnlockPolicy.IsSkinStoryUnlockedByGame(999, OwnedSkins));
    }

    [Fact]
    public void 皮肤CG_绕过关闭_保持游戏原语义()
    {
        // 独立真值表（非用实现重算）：bypass=false 时必须等于原语义
        Assert.Equal(UnlockPolicy.IsSkinStoryUnlockedByGame(101, OwnedSkins), UnlockPolicy.IsSkinStoryUnlocked(101, OwnedSkins, bypassEnabled: false));
        Assert.Equal(UnlockPolicy.IsSkinStoryUnlockedByGame(999, OwnedSkins), UnlockPolicy.IsSkinStoryUnlocked(999, OwnedSkins, bypassEnabled: false));
    }

    [Fact]
    public void 皮肤CG_绕过开启_未持有皮肤也解锁()
    {
        // 核心需求：购买墙被绕过 —— 未持有皮肤也必须为 true
        Assert.True(UnlockPolicy.IsSkinStoryUnlocked(999, OwnedSkins, bypassEnabled: true));
    }

    [Fact]
    public void 皮肤CG_绕过开启_持有皮肤仍解锁()
    {
        Assert.True(UnlockPolicy.IsSkinStoryUnlocked(101, OwnedSkins, bypassEnabled: true));
    }

    // ---- 角色 CG（好感度门槛） ----

    [Fact]
    public void 角色CG_游戏原语义_好感度达标则解锁()
    {
        Assert.True(UnlockPolicy.IsCharacterStoryUnlockedByGame(intimacyLevel: 5, conditionValue: 5));
    }

    [Fact]
    public void 角色CG_游戏原语义_好感度不足则锁定()
    {
        Assert.False(UnlockPolicy.IsCharacterStoryUnlockedByGame(intimacyLevel: 4, conditionValue: 5));
    }

    [Fact]
    public void 角色CG_绕过关闭_保持游戏原语义()
    {
        Assert.Equal(UnlockPolicy.IsCharacterStoryUnlockedByGame(4, 5), UnlockPolicy.IsCharacterStoryUnlocked(4, 5, bypassEnabled: false));
    }

    [Fact]
    public void 角色CG_绕过开启_好感度不足也解锁()
    {
        Assert.True(UnlockPolicy.IsCharacterStoryUnlocked(intimacyLevel: 0, conditionValue: 5, bypassEnabled: true));
    }

    // ---- NTR 屏蔽显示 ----

    [Fact]
    public void NTR屏蔽块_NTR且未无视时显示()
    {
        Assert.True(UnlockPolicy.ShouldShowNtrBlock(isNtr: true, ignoreNtrBlockEnabled: false));
    }

    [Fact]
    public void NTR屏蔽块_无视开关打开时不显示()
    {
        Assert.False(UnlockPolicy.ShouldShowNtrBlock(isNtr: true, ignoreNtrBlockEnabled: true));
    }

    [Fact]
    public void NTR屏蔽块_非NTR场景不显示()
    {
        Assert.False(UnlockPolicy.ShouldShowNtrBlock(isNtr: false, ignoreNtrBlockEnabled: false));
    }
}
