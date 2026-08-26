using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Project.User;
using CharacterDataReadOnlyList = Il2CppSystem.Collections.Generic.IReadOnlyList<Project.User.CharacterData>;
using Il2CppCharacterDataList = Il2CppSystem.Collections.Generic.List<Project.User.CharacterData>;

namespace AbyssCGUnlock;

/// <summary>
/// Stores the current time/account-filtered unowned-character snapshot.
/// No character IDs are encoded by the plugin and every refresh replaces the previous snapshot.
/// </summary>
internal static class LocalCharacterRegistry
{
    private static readonly AccountScopedCharacterCatalog<CharacterData> Catalog =
        new AccountScopedCharacterCatalog<CharacterData>();

    internal static int Replace(UserData userData, CharacterDataReadOnlyList source)
    {
        var ownerKey = OwnerKey(userData);
        var entries = new List<KeyValuePair<long, CharacterData>>();

        if (ownerKey != 0 && source != null)
        {
            // The proxy's IReadOnlyList exposes an IL2CPP interface. Wrap the same native pointer
            // as its concrete List<T> proxy so indexing remains stable across game updates.
            var list = new Il2CppCharacterDataList(IL2CPP.Il2CppObjectBaseToPtr(source));
            entries.Capacity = list.Count;

            for (var i = 0; i < list.Count; i++)
            {
                var character = list[i];
                if (character == null)
                {
                    continue;
                }

                // Synthetic entries must never enter the detail page's native "new" request path.
                character._IsNew_k__BackingField = false;
                character._IsExistStory_k__BackingField = true;
                entries.Add(new KeyValuePair<long, CharacterData>(character._Id_k__BackingField, character));
            }
        }

        Catalog.Replace(ownerKey, entries);
        return entries.Count;
    }

    internal static int Replace(UserData userData, System.Collections.Generic.IReadOnlyList<CharacterData> source)
    {
        var ownerKey = OwnerKey(userData);
        var entries = new List<KeyValuePair<long, CharacterData>>();

        if (ownerKey != 0 && source != null)
        {
            entries.Capacity = source.Count;

            for (var i = 0; i < source.Count; i++)
            {
                var character = source[i];
                if (character == null)
                {
                    continue;
                }

                character._IsNew_k__BackingField = false;
                character._IsExistStory_k__BackingField = true;
                entries.Add(new KeyValuePair<long, CharacterData>(character._Id_k__BackingField, character));
            }
        }

        Catalog.Replace(ownerKey, entries);
        return entries.Count;
    }

    internal static void Clear(UserData userData)
    {
        Catalog.Replace(OwnerKey(userData), Array.Empty<KeyValuePair<long, CharacterData>>());
    }

    internal static bool TryGet(UserData userData, long characterId, out CharacterData character)
    {
        return Catalog.TryGet(OwnerKey(userData), characterId, out character);
    }

    internal static System.Collections.Generic.IReadOnlyList<CharacterData> Snapshot(UserData userData)
    {
        return Catalog.Snapshot(OwnerKey(userData));
    }

    private static nint OwnerKey(UserData userData)
    {
        return userData == null ? 0 : IL2CPP.Il2CppObjectBaseToPtr(userData);
    }
}
