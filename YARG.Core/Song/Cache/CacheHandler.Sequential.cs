using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Extensions;
using YARG.Core.IO;
using YARG.Core.Logging;

namespace YARG.Core.Song.Cache
{
    internal sealed class SequentialCacheHandler : CacheHandler
    {
        internal SequentialCacheHandler(List<string> baseDirectories, bool allowDuplicates, bool fullDirectoryPlaylists)
            : base(baseDirectories, allowDuplicates, fullDirectoryPlaylists) { }

        protected override void FindNewEntries()
        {
            var tracker = new PlaylistTracker(_fullDirectoryPlaylists, null);
            foreach (var group in _iniGroups)
            {
                var dirInfo = new DirectoryInfo(group.Directory);
                ScanDirectory(dirInfo, group, tracker);
            }

            unsafe void ScanCONGroup<TGroup, TEntry>(TGroup group, delegate*<TGroup, string, DTAEntry, SongUpdate?, (DTAEntry? entry, RBProUpgrade? node), (ScanResult, TEntry?)> func)
                where TGroup : CONGroup<TEntry>
                where TEntry : RBCONEntry
            {
                foreach (var node in group.DTAEntries)
                {
                    // If found, then the entry was added during the cache read process
                    if (!group.SongEntries.TryGetValue(node.Key, out var currEntry))
                    {
                        _updates.TryGetValue(node.Key, out var update);
                        _upgrades.TryGetValue(node.Key, out var upgrade);
                        var (result, entry) = func(group, node.Key, node.Value, update, upgrade);
                        if (result != ScanResult.Success)
                        {
                            AddToBadSongs(group.Location + $" - Node {node.Key}", result);
                        }
                        else if (AddEntry(entry!))
                        {
                            group.SongEntries.Add(node.Key, entry!);
                        }
                    }
                    // A failure to add to the combined list means this instance
                    // is seen as a duplicate
                    else if (!AddEntry(currEntry))
                    {
                        // And a rejection means we need to keep it from bring cached
                        // ... for now.
                        group.SongEntries.Remove(node.Key);
                    }
                }
            }

            foreach (var group in _conGroups)
            {
                using var stream = group.Stream = new FileStream(group.Info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
                unsafe
                {
                    ScanCONGroup(group, &PackedRBCONEntry.ProcessNewEntry);
                }
            }

            foreach (var group in _extractedConGroups)
            {
                unsafe
                {
                    ScanCONGroup(group, &UnpackedRBCONEntry.ProcessNewEntry);
                }
            }
        }

        protected override void TraverseDirectory(in FileCollection collection, IniGroup group, PlaylistTracker tracker)
        {
            foreach (var subDirectory in collection.SubDirectories)
            {
                ScanDirectory(subDirectory.Value, group, tracker);
            }

            foreach (var file in collection.Subfiles)
            {
                ScanFile(file.Value, group, in tracker);
            }
        }

        protected override void SortEntries()
        {
            foreach (var node in _cache.Entries)
            {
                foreach (var entry in node.Value)
                {
                    CategorySorter<string, TitleConfig>.Add(entry, _cache.Titles);
                    CategorySorter<SortString, ArtistConfig>.Add(entry, _cache.Artists);
                    CategorySorter<SortString, AlbumConfig>.Add(entry, _cache.Albums);
                    CategorySorter<SortString, GenreConfig>.Add(entry, _cache.Genres);
                    CategorySorter<string, YearConfig>.Add(entry, _cache.Years);
                    CategorySorter<SortString, CharterConfig>.Add(entry, _cache.Charters);
                    CategorySorter<SortString, PlaylistConfig>.Add(entry, _cache.Playlists);
                    CategorySorter<SortString, SourceConfig>.Add(entry, _cache.Sources);
                    CategorySorter<SortString, ArtistAlbumConfig>.Add(entry, _cache.ArtistAlbums);
                    CategorySorter<string, SongLengthConfig>.Add(entry, _cache.SongLengths);
                    CategorySorter<DateTime, DateAddedConfig>.Add(entry, _cache.DatesAdded);
                    InstrumentSorter.Add(entry, _cache.Instruments);
                }
            }
        }

        protected override void Deserialize(UnmanagedMemoryStream stream)
        {
            CategoryCacheStrings strings = new(stream, false);
            RunEntryTasks(stream, strings, ReadIniGroup);
            RunCONTasks(stream, ReadUpdateDirectory);
            RunCONTasks(stream, ReadUpgradeDirectory);
            RunCONTasks(stream, ReadUpgradeCON);
            RunEntryTasks(stream, strings, ReadPackedCONGroup);
            RunEntryTasks(stream, strings, ReadUnpackedCONGroup);
        }

        protected override void Deserialize_Quick(UnmanagedMemoryStream stream)
        {
            CategoryCacheStrings strings = new(stream, false);
            RunEntryTasks(stream, strings, QuickReadIniGroup);

            int skipLength = stream.Read<int>(Endianness.Little);
            stream.Position += skipLength;

            RunCONTasks(stream, QuickReadUpgradeDirectory);
            RunCONTasks(stream, QuickReadUpgradeCON);
            RunEntryTasks(stream, strings, QuickReadCONGroup);
            RunEntryTasks(stream, strings, QuickReadExtractedCONGroup);
        }

        protected override void AddUpdate(string name, SongUpdate update)
        {
            if (_updates.TryGetValue(name, out var currentUpdate))
            {
                update = SongUpdate.Union(update, currentUpdate);
            }
            _updates[name] = update;
        }

        protected override void AddUpgrade(string name, DTAEntry? entry, RBProUpgrade upgrade)
        {
            _upgrades[name] = new(entry, upgrade);
        }

        protected override void AddPackedCONGroup(PackedCONGroup group)
        {
            _conGroups.Add(group);
        }

        protected override void AddUnpackedCONGroup(UnpackedCONGroup group)
        {
            _extractedConGroups.Add(group);
        }

        protected override void AddUpdateGroup(UpdateGroup group)
        {
            _updateGroups.Add(group);
        }

        protected override void AddUpgradeGroup(UpgradeGroup group)
        {
            _upgradeGroups.Add(group);
        }

        protected override void AddCollectionToCache(in FileCollection collection)
        {
            collectionCache.Add(collection.Directory.FullName, collection);
        }

        protected override void RemoveCONEntry(string shortname)
        {
            foreach (var group in _conGroups)
            {
                if (group.SongEntries.Remove(shortname))
                {
                    YargLogger.LogFormatTrace("{0} - {1} pending rescan", group.Location, item2: shortname);
                }
            }

            foreach (var group in _extractedConGroups)
            {
                if (group.SongEntries.Remove(shortname))
                {
                    YargLogger.LogFormatTrace("{0} - {1} pending rescan", group.Location, item2: shortname);
                }
            }
        }

        protected override bool CanAddUpgrade(string shortname, DateTime lastUpdated)
        {
            foreach (var group in _upgradeGroups)
            {
                if (group.Upgrades.TryGetValue(shortname, out var currUpgrade) && currUpgrade.Upgrade != null)
                {
                    if (currUpgrade.Upgrade.LastUpdatedTime >= lastUpdated)
                    {
                        return false;
                    }
                    group.Upgrades.Remove(shortname);
                    return true;
                }
            }
            return false;
        }

        protected override bool CanAddUpgrade_CONInclusive(string shortname, DateTime lastUpdated)
        {
            foreach (var group in _conGroups)
            {
                if (group.Upgrades.TryGetValue(shortname, out var currUpgrade) && currUpgrade.Upgrade != null)
                {
                    if (currUpgrade.Upgrade.LastUpdatedTime >= lastUpdated)
                    {
                        return false;
                    }
                    group.Upgrades.Remove(shortname);
                    return true;
                }
            }
            return CanAddUpgrade(shortname, lastUpdated);
        }

        protected override Dictionary<string, Dictionary<string, FileInfo>> MapUpdateFiles(in FileCollection collection)
        {
            Dictionary<string, Dictionary<string, FileInfo>> mapping = new();
            foreach (var dir in collection.SubDirectories)
            {
                var infos = new Dictionary<string, FileInfo>();
                foreach (var file in dir.Value.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    infos[file.Name] = file;
                }
                mapping[dir.Key] = infos;
            }
            return mapping;
        }

        protected override PackedCONGroup? FindCONGroup(string filename)
        {
            return _conGroups.Find(node => node.Location == filename);
        }

        protected override void CleanupDuplicates()
        {
            foreach (var entry in _duplicatesToRemove)
            {
                if (TryRemove<IniGroup, IniSubEntry>(_iniGroups, entry))
                {
                    continue;
                }

                if (TryRemove<PackedCONGroup, RBCONEntry>(_conGroups, entry))
                {
                    continue;
                }

                TryRemove<UnpackedCONGroup, RBCONEntry>(_extractedConGroups, entry);
            }
        }

        private void ReadIniGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            string directory = stream.ReadString();
            var group = GetBaseIniGroup(directory);
            if (group == null)
            {
                return;
            }

            int count = stream.Read<int>(Endianness.Little);
            for (int i = 0; i < count; ++i)
            {
                int length = stream.Read<int>(Endianness.Little);
                var slice = stream.Slice(length);
                ReadIniEntry(group, directory, slice, strings);
            }
        }

        private void ReadPackedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var group = ReadCONGroupHeader(stream);
            if (group != null)
            {
                int count = stream.Read<int>(Endianness.Little);
                for (int i = 0; i < count; ++i)
                {
                    string name = stream.ReadString();
                    int length = stream.Read<int>(Endianness.Little);
                    if (invalidSongsInCache.Contains(name))
                    {
                        stream.Position += length;
                        continue;
                    }

                    using var entryReader = stream.Slice(length);
                    var song = PackedRBCONEntry.TryLoadFromCache(in group.ConFile, name, _upgrades, entryReader, strings);
                    if (song != null)
                    {
                        lock (group.SongEntries)
                        {
                            group.SongEntries.Add(name, song);
                        }
                    }
                }
            }
        }

        private void ReadUnpackedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var group = ReadExtractedCONGroupHeader(stream);
            if (group != null)
            {
                int count = stream.Read<int>(Endianness.Little);
                for (int i = 0; i < count; ++i)
                {
                    string name = stream.ReadString();
                    int length = stream.Read<int>(Endianness.Little);
                    if (invalidSongsInCache.Contains(name))
                    {
                        stream.Position += length;
                        continue;
                    }

                    using var entryReader = stream.Slice(length);
                    var song = UnpackedRBCONEntry.TryLoadFromCache(group.Location, group.DTAInfo, name, _upgrades, entryReader, strings);
                    if (song != null)
                    {
                        lock (group.SongEntries)
                        {
                            group.SongEntries.Add(name, song);
                        }
                    }
                }
            }
        }

        private void QuickReadIniGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            string directory = stream.ReadString();
            int count = stream.Read<int>(Endianness.Little);
            for (int i = 0; i < count; ++i)
            {
                int length = stream.Read<int>(Endianness.Little);
                var slice = stream.Slice(length);
                QuickReadIniEntry(directory, slice, strings);
            }
        }

        private void QuickReadCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var group = QuickReadCONGroupHeader(stream);
            if (group == null)
                return;

            int count = stream.Read<int>(Endianness.Little);
            for (int i = 0; i < count; ++i)
            {
                string name = stream.ReadString();
                int length = stream.Read<int>(Endianness.Little);
                var slice = stream.Slice(length);
                AddEntry(PackedRBCONEntry.LoadFromCache_Quick(in group.ConFile, name, _upgrades, slice, strings));
            }
        }

        private void QuickReadExtractedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            string directory = stream.ReadString();
            var dta = new AbridgedFileInfo(Path.Combine(directory, "songs.dta"), stream);

            int count = stream.Read<int>(Endianness.Little);
            for (int i = 0; i < count; ++i)
            {
                string name = stream.ReadString();
                int length = stream.Read<int>(Endianness.Little);
                var slice = stream.Slice(length);
                AddEntry(UnpackedRBCONEntry.LoadFromCache_Quick(directory, dta, name, _upgrades, slice, strings));
            }
        }

        private static void RunCONTasks(UnmanagedMemoryStream stream, Action<UnmanagedMemoryStream> func)
        {
            int sectionLength = stream.Read<int>(Endianness.Little);
            var sectionSlice = stream.Slice(sectionLength);

            int count = sectionSlice.Read<int>(Endianness.Little);
            for (int i = 0; i < count; ++i)
            {
                int groupLength = sectionSlice.Read<int>(Endianness.Little);
                var groupSlice = sectionSlice.Slice(groupLength);
                func(groupSlice);
            }
        }

        private static void RunEntryTasks(UnmanagedMemoryStream stream, CategoryCacheStrings strings, Action<UnmanagedMemoryStream, CategoryCacheStrings> func)
        {
            int sectionLength = stream.Read<int>(Endianness.Little);
            var sectionSlice = stream.Slice(sectionLength);

            int count = sectionSlice.Read<int>(Endianness.Little);
            for (int i = 0; i < count; ++i)
            {
                int groupLength = sectionSlice.Read<int>(Endianness.Little);
                var groupSlice = sectionSlice.Slice(groupLength);
                func(groupSlice, strings);
            }
        }
    }
}
