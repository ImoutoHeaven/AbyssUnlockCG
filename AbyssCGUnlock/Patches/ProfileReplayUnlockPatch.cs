using System.Reflection;
using HarmonyLib;
using Project.Interaction.ProfileMode;
using Project.Master.NoaMessagePack;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Clears the client-side lock as each Profile -> Other -> Performance model is created.
/// The profile initialization state machine writes the controller fields directly and does not call
/// ProfileReplayViewController.UpdateView, so the model factory is the first stable shared seam for
/// both owned and registry-backed unowned characters. Voice models use another overload and remain untouched.
/// </summary>
internal static class ProfileReplayUnlockPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(ProfileReplayListModel),
            nameof(ProfileReplayListModel.Create),
            new[]
            {
                typeof(bool),
                typeof(MNovelHomes),
                typeof(EventType),
                typeof(int),
            });
    }

    internal static void Postfix(ProfileReplayListModel __result)
    {
        if (__result == null)
        {
            return;
        }

        var wasLocked = __result._IsLocked_k__BackingField;
        __result._IsLocked_k__BackingField = UnlockPolicy.IsProfileReplayLocked(
            wasLocked,
            PluginConfig.EnableProfileReplayUnlock.Value);

        if (wasLocked && !__result._IsLocked_k__BackingField)
        {
            CgUnlockPlugin.LogSource.LogInfo(
                $"[CGUnlock] 角色个人资料演出已强制本地解锁(无视羁绊门槛): event_type={__result.EventType}");
        }
    }
}
