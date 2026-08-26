using System;
using System.Collections.Generic;

namespace AbyssCGUnlock;

/// <summary>
/// Pure selection policy for the current master-cache/account snapshot.
/// The key is a master character ID and the value records whether it is open at server time.
/// </summary>
internal static class DynamicUnownedCharacterSelector
{
    internal static IReadOnlyList<long> Select(
        IEnumerable<KeyValuePair<long, bool>> candidates,
        ISet<long> ownedMasterIds)
    {
        if (candidates == null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (ownedMasterIds == null)
        {
            throw new ArgumentNullException(nameof(ownedMasterIds));
        }

        var selected = new List<long>();
        var seen = new HashSet<long>();

        foreach (var candidate in candidates)
        {
            if (!candidate.Value || ownedMasterIds.Contains(candidate.Key) || !seen.Add(candidate.Key))
            {
                continue;
            }

            selected.Add(candidate.Key);
        }

        return selected;
    }
}
