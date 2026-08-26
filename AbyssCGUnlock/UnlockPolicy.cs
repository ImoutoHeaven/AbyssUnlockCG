using System.Collections.Generic;
using System.Linq;

namespace AbyssCGUnlock;

/// <summary>
/// 游戏解锁语义的纯函数镜像（可单测）。
/// 证据：evidence/cpp2il/IsilDump/Project/Project/Interaction/AdventurerDetail/R18StoryListViewController_NestedType___c__DisplayClass11_0.txt
/// </summary>
public static class UnlockPolicy
{
    /// <summary>游戏原语义（皮肤卧室 CG）：持有皮肤 id 才解锁。</summary>
    public static bool IsSkinStoryUnlockedByGame(int mCharacterSkinId, IReadOnlyCollection<long> userSkinIds)
        => userSkinIds.Contains(mCharacterSkinId);

    /// <summary>插件语义：绕过开关打开时无条件解锁。</summary>
    public static bool IsSkinStoryUnlocked(int mCharacterSkinId, IReadOnlyCollection<long> userSkinIds, bool bypassEnabled)
        => bypassEnabled || IsSkinStoryUnlockedByGame(mCharacterSkinId, userSkinIds);

    /// <summary>游戏原语义（角色 CG）：intimacyLevel &gt;= condition_value 才解锁。</summary>
    public static bool IsCharacterStoryUnlockedByGame(int intimacyLevel, int conditionValue)
        => intimacyLevel >= conditionValue;

    /// <summary>插件语义：绕过开关打开时无条件解锁。</summary>
    public static bool IsCharacterStoryUnlocked(int intimacyLevel, int conditionValue, bool bypassEnabled)
        => bypassEnabled || IsCharacterStoryUnlockedByGame(intimacyLevel, conditionValue);

    /// <summary>NTR 屏蔽块是否应显示：IsNtr 且未启用「无视屏蔽」。</summary>
    public static bool ShouldShowNtrBlock(bool isNtr, bool ignoreNtrBlockEnabled)
        => isNtr && !ignoreNtrBlockEnabled;
}
