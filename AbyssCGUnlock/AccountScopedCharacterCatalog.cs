using System;
using System.Collections.Generic;

namespace AbyssCGUnlock;

/// <summary>
/// One replace-only character snapshot bound to the native identity of the current UserData.
/// Replacing instead of merging prevents stale characters leaking across list refreshes or accounts.
/// </summary>
internal sealed class AccountScopedCharacterCatalog<TValue>
{
    private readonly object _gate = new object();
    private readonly Dictionary<long, TValue> _entries = new Dictionary<long, TValue>();
    private nint _ownerKey;

    internal void Replace(nint ownerKey, IEnumerable<KeyValuePair<long, TValue>> entries)
    {
        lock (_gate)
        {
            _ownerKey = ownerKey;
            _entries.Clear();

            if (ownerKey == 0 || entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                _entries[entry.Key] = entry.Value;
            }
        }
    }

    internal bool TryGet(nint ownerKey, long characterId, out TValue value)
    {
        lock (_gate)
        {
            if (ownerKey == 0 || ownerKey != _ownerKey)
            {
                value = default!;
                return false;
            }

            if (_entries.TryGetValue(characterId, out var found))
            {
                value = found;
                return true;
            }

            value = default!;
            return false;
        }
    }

    internal IReadOnlyList<TValue> Snapshot(nint ownerKey)
    {
        lock (_gate)
        {
            if (ownerKey == 0 || ownerKey != _ownerKey)
            {
                return Array.Empty<TValue>();
            }

            return new List<TValue>(_entries.Values);
        }
    }
}
