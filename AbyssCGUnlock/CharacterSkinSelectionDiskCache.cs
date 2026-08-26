#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AbyssCGUnlock;

/// <summary>
/// Versioned, account-scoped persistence for local-only skin selections. The cache contains only
/// numeric account/character/skin identities and is rewritten atomically after each confirmation.
/// </summary>
internal sealed class CharacterSkinSelectionDiskCache
{
    private const string Header = "AbyssCGUnlock.CharacterSkinSelections\t1";

    private readonly object _gate = new();
    private readonly string _path;
    private readonly Dictionary<long, Dictionary<long, CharacterSkinSelection>> _selections = new();
    private bool _loaded;

    internal CharacterSkinSelectionDiskCache(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Cache path is required.", nameof(path));
        }

        _path = path;
    }

    internal bool TryGet(
        long accountId,
        long characterId,
        out CharacterSkinSelection selection)
    {
        if (accountId <= 0 || characterId <= 0)
        {
            selection = default;
            return false;
        }

        lock (_gate)
        {
            try
            {
                EnsureLoaded();
                if (_selections.TryGetValue(accountId, out var accountSelections) &&
                    accountSelections.TryGetValue(characterId, out selection))
                {
                    return true;
                }
            }
            catch
            {
                // Cache I/O must never prevent the in-memory local skin path from operating.
            }

            selection = default;
            return false;
        }
    }

    internal bool Save(
        long accountId,
        long characterId,
        CharacterSkinSelection selection)
    {
        if (accountId <= 0 ||
            characterId <= 0 ||
            (selection.BattleSkinId <= 0 && selection.TavernSkinId <= 0))
        {
            return false;
        }

        lock (_gate)
        {
            try
            {
                EnsureLoaded();
                if (!_selections.TryGetValue(accountId, out var accountSelections))
                {
                    accountSelections = new Dictionary<long, CharacterSkinSelection>();
                    _selections.Add(accountId, accountSelections);
                }

                accountSelections[characterId] = selection;
                Persist();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (!File.Exists(_path))
        {
            return;
        }

        foreach (var line in File.ReadLines(_path))
        {
            var columns = line.Split('\t');
            if (columns.Length != 4 ||
                !TryParsePositive(columns[0], out var accountId) ||
                !TryParsePositive(columns[1], out var characterId) ||
                !TryParseNonNegative(columns[2], out var battleSkinId) ||
                !TryParseNonNegative(columns[3], out var tavernSkinId) ||
                (battleSkinId == 0 && tavernSkinId == 0))
            {
                continue;
            }

            if (!_selections.TryGetValue(accountId, out var accountSelections))
            {
                accountSelections = new Dictionary<long, CharacterSkinSelection>();
                _selections.Add(accountId, accountSelections);
            }

            accountSelections[characterId] =
                new CharacterSkinSelection(battleSkinId, tavernSkinId);
        }
    }

    private void Persist()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.WriteLine(Header);
                foreach (var account in _selections.OrderBy(entry => entry.Key))
                {
                    foreach (var character in account.Value.OrderBy(entry => entry.Key))
                    {
                        writer.Write(account.Key.ToString(CultureInfo.InvariantCulture));
                        writer.Write('\t');
                        writer.Write(character.Key.ToString(CultureInfo.InvariantCulture));
                        writer.Write('\t');
                        writer.Write(character.Value.BattleSkinId.ToString(CultureInfo.InvariantCulture));
                        writer.Write('\t');
                        writer.WriteLine(character.Value.TavernSkinId.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool TryParsePositive(string value, out long result)
    {
        return long.TryParse(
                   value,
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out result) &&
               result > 0;
    }

    private static bool TryParseNonNegative(string value, out long result)
    {
        return long.TryParse(
                   value,
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out result) &&
               result >= 0;
    }
}
