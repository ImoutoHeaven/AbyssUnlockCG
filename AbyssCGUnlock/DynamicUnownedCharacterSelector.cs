using System;
using System.Collections.Generic;

namespace AbyssCGUnlock;

/// <summary>
/// Pure selection for one master-cache and account snapshot.
/// The public client list is not consulted: open_at does not remove a candidate.
/// </summary>
internal static class DynamicUnownedCharacterSelector
{
    internal static IReadOnlyList<long> Select(IEnumerable<long> candidateIds, ISet<long> ownedMasterIds)
    {
        if (candidateIds == null)
        {
            throw new ArgumentNullException(nameof(candidateIds));
        }

        if (ownedMasterIds == null)
        {
            throw new ArgumentNullException(nameof(ownedMasterIds));
        }

        var selected = new List<long>();
        var seen = new HashSet<long>();

        foreach (var candidateId in candidateIds)
        {
            if (ownedMasterIds.Contains(candidateId) || !seen.Add(candidateId))
            {
                continue;
            }

            selected.Add(candidateId);
        }

        return selected;
    }

    /// <summary>
    /// Records required before CreateFromMaster and AdventurerDetail.UpdateView.
    /// ponytail: this is the master-row gate, not a check that addressable bundles are on disk.
    /// </summary>
    internal static bool CanLocallyUnlock(
        bool hasUnionType,
        bool hasDefaultBattleSkinAsset,
        bool hasProfile,
        bool hasTavernWorkSkin)
        => hasUnionType && hasDefaultBattleSkinAsset && hasProfile && hasTavernWorkSkin;
}
