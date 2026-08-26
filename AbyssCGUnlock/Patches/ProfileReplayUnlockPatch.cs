using System.Reflection;
using HarmonyLib;
using Project.Interaction.ProfileMode;
using Project.User;
using ProfileReplayModelList = Il2CppSystem.Collections.Generic.List<Project.Interaction.ProfileMode.ProfileReplayListModel>;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Clears only the client-side lock on Profile -> Other -> Performance entries for both owned and
/// registry-backed unowned characters before the native controller copies and renders the supplied
/// event list. Voice entries are intentionally untouched.
/// </summary>
internal static class ProfileReplayUnlockPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(ProfileReplayViewController),
            nameof(ProfileReplayViewController.UpdateView),
            new[]
            {
                typeof(ProfileReplayModelList),
                typeof(ProfileReplayModelList),
                typeof(CharacterData),
            });
    }

    internal static void Prefix(ProfileReplayModelList eventList)
    {
        if (!PluginConfig.EnableProfileReplayUnlock.Value || eventList == null)
        {
            return;
        }

        var unlocked = 0;
        for (var i = 0; i < eventList.Count; i++)
        {
            var model = eventList[i];
            if (model == null || !model.IsLocked)
            {
                continue;
            }

            model.IsLocked = false;
            unlocked++;
        }

        if (unlocked > 0)
        {
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 角色个人资料演出已强制本地解锁(无视羁绊门槛): count={unlocked}");
        }
    }
}
