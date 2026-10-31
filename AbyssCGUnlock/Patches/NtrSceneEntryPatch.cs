using System.Reflection;
using HarmonyLib;
using Project.User;
using UniRx;
using UnityEngine;
using DetailSubService = Project.Interaction.AdventurerDetail.SubService;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// Bypasses the client-only tavern-registration overlay at the exact NTR-mode callback.
/// The backing field is restored in a Harmony Finalizer even when the native callback throws.
///
/// The callback lives in a compiler-generated closure class, so the class index is renumbered by every
/// game update (&lt;&gt;c__DisplayClass90_0 became 91_0 on 2026-10-30). The lambda itself keeps its name
/// and signature, so it is resolved by those at precheck.
/// </summary>
internal static class NtrSceneEntryPatch
{
    internal readonly struct TavernGateState
    {
        internal TavernGateState(CharacterData character, bool originalValue)
        {
            Character = character;
            OriginalValue = originalValue;
        }

        internal CharacterData Character { get; }
        internal bool OriginalValue { get; }
        internal bool Applied => Character != null;
    }

    internal static MethodBase TargetMethod()
    {
        return GeneratedTargets.FindClosureMethod(
            typeof(DetailSubService),
            "_InitializeViewAsync_b__6",
            new[] { typeof(Unit), typeof(DetailSubService), typeof(GameObject) });
    }

    internal static void Prefix(DetailSubService service, out TavernGateState __state)
    {
        __state = default;

        if (!PluginConfig.EnableNtrSceneEntryBypass.Value || service == null)
        {
            return;
        }

        var character = service._charaData;
        if (character == null || character._IsTavernRegistered_k__BackingField)
        {
            return;
        }

        __state = new TavernGateState(character, character._IsTavernRegistered_k__BackingField);
        character._IsTavernRegistered_k__BackingField = true;
    }

    internal static System.Exception Finalizer(System.Exception __exception, TavernGateState __state)
    {
        if (__state.Applied)
        {
            __state.Character._IsTavernRegistered_k__BackingField = __state.OriginalValue;
        }

        return __exception;
    }
}
