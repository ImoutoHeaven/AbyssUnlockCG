using AbyssCGUnlock.Patches;
using Xunit;

namespace AbyssCGUnlock.Tests;

public class LocalAbilityTableIdTests
{
    [Fact]
    public void 账号行保留原TableId()
    {
        Assert.Equal(11, CharacterAbilityLocalViewPatch.ResolveLocalAbilityTableId(11, 101));
    }

    [Fact]
    public void Master合成行TableId为0时改用该行MasterId()
    {
        Assert.Equal(101, CharacterAbilityLocalViewPatch.ResolveLocalAbilityTableId(0, 101));
        Assert.Equal(202, CharacterAbilityLocalViewPatch.ResolveLocalAbilityTableId(0, 202));
        Assert.Equal(303, CharacterAbilityLocalViewPatch.ResolveLocalAbilityTableId(0, 303));
    }
}
