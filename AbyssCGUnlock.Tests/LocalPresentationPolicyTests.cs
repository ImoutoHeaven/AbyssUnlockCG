using AbyssCGUnlock;
using Xunit;

namespace AbyssCGUnlock.Tests;

public class LocalPresentationPolicyTests
{
    [Theory]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, false)]
    public void 寝室剧情_对应本地绕过开启或游戏原本解锁时最终可用(
        bool gameUnlocked,
        bool isSkinStory,
        bool characterBypassEnabled,
        bool skinBypassEnabled)
    {
        Assert.True(UnlockPolicy.IsBedroomStoryUnlocked(
            gameUnlocked,
            isSkinStory,
            characterBypassEnabled,
            skinBypassEnabled));
    }

    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    public void 寝室剧情_关闭对应绕过时不借用另一类开关(
        bool gameUnlocked,
        bool isSkinStory,
        bool characterBypassEnabled,
        bool skinBypassEnabled)
    {
        Assert.False(UnlockPolicy.IsBedroomStoryUnlocked(
            gameUnlocked,
            isSkinStory,
            characterBypassEnabled,
            skinBypassEnabled));
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void 个人资料演出_绕过开启时清除锁定_关闭时保留游戏状态(
        bool gameLocked,
        bool bypassEnabled,
        bool expectedLocked)
    {
        Assert.Equal(expectedLocked, UnlockPolicy.IsProfileReplayLocked(gameLocked, bypassEnabled));
    }
}
