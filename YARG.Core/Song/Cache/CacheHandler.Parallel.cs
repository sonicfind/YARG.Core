using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using YARG.Core.Extensions;
using YARG.Core.IO;
using YARG.Core.Logging;

namespace YARG.Core.Song.Cache
{
    internal sealed class ParallelCacheHandler : CacheHandler
    {
        internal ParallelCacheHandler(List<string> baseDirectories, bool allowDuplicates, bool fullDirectoryPlaylists)
            : base(baseDirectories, allowDuplicates, fullDirectoryPlaylists) { }

        protected override void FindNewEntries()
        {
            var tracker = new PlaylistTracker(_fullDirectoryPlaylists, null);
            Parallel.ForEach(_iniGroups, group =>
            {
                var dirInfo = new DirectoryInfo(group.Directory);
                ScanDirectory(dirInfo, group, tracker);
            });

            unsafe void ScanCONGroup<TGroup, TEntry>(TGroup group, delegate*<TGroup, string, DTAEntry, SongUpdate?, (DTAEntry? entry, RBProUpgrade? node), (ScanResult, TEntry?)> func)
                where TGroup : CONGroup<TEntry>
                where TEntry : RBCONEntry
            {
                bool TryGetLocked(string name, out TEntry entry)
                {
                    lock (group.SongEntries)
                    {
                        return group.SongEntries.TryGetValue(name, out entry);
                    }
                }

                Parallel.ForEach(group.DTAEntries, node =>
                {
                    // If found, then the entry was added during the cache read process
                    if (!TryGetLocked(node.Key, out var currEntry))
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
                            lock (group.SongEntries)
                            {
                                group.SongEntries.Add(node.Key, entry!);
                            }
                        }
                    }
                    // A failure to add to the combined list means this instance
                    // is seen as a duplicate
                    else if (!AddEntry(currEntry))
                    {
                        // And a rejection means we need to keep it from bring cached
                        // ... for now.
                        lock (group.SongEntries)
                        {
                            group.SongEntries.Remove(node.Key);
                        }
                    }
                });
            }

            var conActions = new Action[_conGroups.Count + _extractedConGroups.Count];
            for (int i = 0; i < _conGroups.Count; ++i)
            {
                var group = _conGroups[i];
                conActions[i] = () =>
                {
                    using var stream = group.Stream = new FileStream(group.Info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
                    unsafe
                    {
                        ScanCONGroup(group, &PackedRBCONEntry.ProcessNewEntry);
                    }
                };
            }

            for (int i = 0; i < _extractedConGroups.Count; ++i)
            {
                var group = _extractedConGroups[i];
                unsafe
                {
                    conActions[_conGroups.Count + i] = () => ScanCONGroup(group, &UnpackedRBCONEntry.ProcessNewEntry);
                }
            }
            Parallel.ForEach(conActions, action => action());
        }

        protected override void TraverseDirectory(in FileCollection collection, IniGroup group, PlaylistTracker tracker)
        {
            var actions = new Action[collection.SubDirectories.Count + collection.Subfiles.Count];
            int index = 0;
            foreach (var directory in collection.SubDirectories)
            {
                actions[index++] = () => ScanDirectory(directory.Value, group, tracker);
            }

            foreach (var file in collection.Subfiles)
            {
                actions[index++] = () => ScanFile(file.Value, group, in tracker);
            }
            Parallel.ForEach(actions, action => action());
        }

        protected override bool AddEntry(SongEntry entry)
        {
            lock (_cache.Entries)
            {
                return base.AddEntry(entry);
            }
        }

        protected override void SortEntries()
        {
            Parallel.ForEach(_cache.Entries, node =>
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
            });
        }

        protected override void Deserialize(UnmanagedMemoryStream stream)
        {
            CategoryCacheStrings strings = new(stream, true);
            var tracker = new ParallelExceptionTracker();
            var entryTasks = new List<Task>();
            var conTasks = new List<Task>();

            try
            {
                AddParallelEntryTasks(stream, entryTasks, strings, ReadIniGroup, tracker);
                AddParallelCONTasks(stream, conTasks, ReadUpdateDirectory, tracker);
                AddParallelCONTasks(stream, conTasks, ReadUpgradeDirectory, tracker);
                AddParallelCONTasks(stream, conTasks, ReadUpgradeCON, tracker);
                Task.WaitAll(conTasks.ToArray());

                AddParallelEntryTasks(stream, entryTasks, strings, ReadPackedCONGroup, tracker);
                AddParallelEntryTasks(stream, entryTasks, strings, ReadUnpackedCONGroup, tracker);
            }
            catch (Exception ex)
            {
                tracker.Set(ex);
                Task.WaitAll(conTasks.ToArray());
            }
            Task.WaitAll(entryTasks.ToArray());

            if (tracker.IsSet())
                throw tracker;
        }

        protected override void Deserialize_Quick(UnmanagedMemoryStream stream)
        {
            YargLogger.LogDebug("Quick Read start");
            CategoryCacheStrings strings = new(stream, true);
            var tracker = new ParallelExceptionTracker();
            var entryTasks = new List<Task>();
            var conTasks = new List<Task>();

            try
            {
                AddParallelEntryTasks(stream, entryTasks, strings, QuickReadIniGroup, tracker);

                int skipLength = stream.Read<int>(Endianness.Little);
                stream.Position += skipLength;

                AddParallelCONTasks(stream, conTasks, QuickReadUpgradeDirectory, tracker);
                AddParallelCONTasks(stream, conTasks, QuickReadUpgradeCON, tracker);
                Task.WaitAll(conTasks.ToArray());

                AddParallelEntryTasks(stream, entryTasks, strings, QuickReadCONGroup, tracker);
                AddParallelEntryTasks(stream, entryTasks, strings, QuickReadExtractedCONGroup, tracker);
            }
            catch (Exception ex)
            {
                tracker.Set(ex);
                Task.WaitAll(conTasks.ToArray());
            }
            Task.WaitAll(entryTasks.ToArray());

            if (tracker.IsSet())
            {
                throw tracker;
            }
        }

        protected override void AddUpdate(string name, SongUpdate update)
        {
            lock (_updates)
            {
                if (_updates.TryGetValue(name, out var currentUpdate))
                {
                    update = SongUpdate.Union(update, currentUpdate);
                }
                _updates[name] = update;
            }
        }

        protected override void AddUpgrade(string name, DTAEntry? entry, RBProUpgrade upgrade)
        {
            lock (_upgrades)
            {
                _upgrades[name] = new(entry, upgrade);
            }
        }

        protected override void AddPackedCONGroup(PackedCONGroup group)
        {
            lock (_conGroups)
            {
                _conGroups.Add(group);
            }
        }

        protected override void AddUnpackedCONGroup(UnpackedCONGroup group)
        {
            lock (_extractedConGroups)
            {
                _extractedConGroups.Add(group);
            }
        }

        protected override void AddUpdateGroup(UpdateGroup group)
        {
            lock (_updateGroups)
            {
                _updateGroups.Add(group);
            }
        }

        protected override void AddUpgradeGroup(UpgradeGroup group)
        {
            lock (_upgradeGroups)
            {
                _upgradeGroups.Add(group);
            }
        }

        protected override void AddCollectionToCache(in FileCollection collection)
        {
            lock (collectionCache)
            {
                collectionCache.Add(collection.Directory.FullName, collection);
            }
        }

        protected override void RemoveCONEntry(string shortname)
        {
            lock (_conGroups)
            {
                foreach (var group in _conGroups)
                {
                    if (group.SongEntries.Remove(shortname))
                    {
                        YargLogger.LogFormatTrace("{0} - {1} pending rescan", group.Location, item2: shortname);
                    }
                }
            }

            lock (_extractedConGroups)
            {
                foreach (var group in _extractedConGroups)
                {
                    if (group.SongEntries.Remove(shortname))
                    {
                        YargLogger.LogFormatTrace("{0} - {1} pending rescan", group.Location, item2: shortname);
                    }
                }
            }
        }

        protected override bool CanAddUpgrade(string shortname, DateTime lastUpdated)
        {
            lock (_upgradeGroups)
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
        }

        protected override bool CanAddUpgrade_CONInclusive(string shortname, DateTime lastUpdated)
        {
            lock (_conGroups)
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
            }
            return CanAddUpgrade(shortname, lastUpdated);
        }

        protected override Dictionary<string, Dictionary<string, FileInfo>> MapUpdateFiles(in FileCollection collection)
        {
            Dictionary<string, Dictionary<string, FileInfo>> mapping = new();
            Parallel.ForEach(collection.SubDirectories, dir =>
            {
                var infos = new Dictionary<string, FileInfo>();
                foreach (var file in dir.Value.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    infos[file.Name] = file;
                }

                lock (mapping)
                {
                    mapping.Add(dir.Key, infos);
                }
            });
            return mapping;
        }

        protected override bool FindOrMarkDirectory(string directory)
        {
            lock (_preScannedDirectories)
            {
                return base.FindOrMarkDirectory(directory);
            }
        }

        protected override bool FindOrMarkFile(string file)
        {
            lock (_preScannedFiles)
            {
                return base.FindOrMarkFile(file);
            }
        }

        protected override void AddToBadSongs(string filePath, ScanResult err)
        {
            lock (_badSongs)
            {
                base.AddToBadSongs(filePath, err);
            }
        }

        protected override void AddInvalidSong(string name)
        {
            lock (invalidSongsInCache)
            {
                base.AddInvalidSong(name);
            }
        }

        protected override PackedCONGroup? FindCONGroup(string filename)
        {
            lock (_conGroups)
            {
                return _conGroups.Find(node => node.Location == filename);
            }
        }

        protected override void CleanupDuplicates()
        {
            Parallel.ForEach(_duplicatesToRemove, entry =>
            {
                lock (_iniGroups)
                {
                    if (TryRemove<IniGroup, IniSubEntry>(_iniGroups, entry))
                    {
                        return;
                    }
                }

                lock (_conGroups)
                {
                    if (TryRemove<PackedCONGroup, RBCONEntry>(_conGroups, entry))
                    {
                        return;
                    }
                }

                lock (_extractedConGroups)
                {
                    TryRemove<UnpackedCONGroup, RBCONEntry>(_extractedConGroups, entry);
                }
            });
        }

        private sealed class ParallelExceptionTracker : Exception
        {
            private readonly object _lock = new object();
            private Exception? _exception = null;

            public bool IsSet()
            {
                lock (_lock)
                    return _exception != null;
            }

            /// <summary>
            /// Once set, the exception can not be swapped.
            /// </summary>
            public void Set(Exception exception)
            {
                lock (_lock)
                    _exception ??= exception;
            }

            public Exception? Exception => _exception;

            public override IDictionary? Data => _exception?.Data;

            public override string Message => _exception?.Message ?? string.Empty;

            public override string StackTrace => _exception?.StackTrace ?? string.Empty;

            public override string ToString()
            {
                return _exception?.ToString() ?? string.Empty;
            }

            public override Exception? GetBaseException()
            {
                return _exception?.GetBaseException();
            }

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                _exception?.GetObjectData(info, context);
            }
        }

        private readonly struct CacheEnumerable<T> : IEnumerable<T>
        {
            private readonly UnmanagedMemoryStream _stream;
            private readonly ParallelExceptionTracker _tracker;
            private readonly Func<T?> _creator;

            public CacheEnumerable(UnmanagedMemoryStream stream, ParallelExceptionTracker tracker, Func<T?> creator)
            {
                _stream = stream;
                _tracker = tracker;
                _creator = creator;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private struct Enumerator : IEnumerator<T>, IEnumerator
            {
                private readonly ParallelExceptionTracker _tracker;
                private readonly Func<T?> _creator;

                private readonly int _count;
                private int _index;
                private T _current;

                public Enumerator(CacheEnumerable<T> values)
                {
                    _tracker = values._tracker;
                    _creator = values._creator;
                    _count = values._stream.Read<int>(Endianness.Little);
                    _index = 0;
                    _current = default!;
                }

                public readonly T Current => _current;

                readonly object IEnumerator.Current => _current!;

                public void Dispose()
                {
                    _current = default!;
                }

                public bool MoveNext()
                {
                    while (_index < _count && !_tracker.IsSet())
                    {
                        ++_index;
                        var slice = _creator();
                        if (slice != null)
                        {
                            _current = slice;
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void ReadIniGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            string directory = stream.ReadString();
            var group = GetBaseIniGroup(directory);
            if (group == null)
            {
                return;
            }

            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(stream, tracker, () =>
            {
                int length = stream.Read<int>(Endianness.Little);
                return stream.Slice(length);
            });

            Parallel.ForEach(enumerable, slice =>
            {
                try
                {
                    ReadIniEntry(group, directory, slice, strings);
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private void ReadPackedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            var group = ReadCONGroupHeader(stream);
            if (group == null)
            {
                return;
            }

            var enumerable = new CacheEnumerable<(string Name, UnmanagedMemoryStream Stream)?>(stream, tracker, () =>
            {
                string name = stream.ReadString();
                int length = stream.Read<int>(Endianness.Little);
                if (invalidSongsInCache.Contains(name))
                {
                    stream.Position += length;
                    return null;
                }
                return (name, stream.Slice(length));
            });

            Parallel.ForEach(enumerable, slice =>
            {
                var value = slice!.Value;
                // Error catching must be done per-thread
                try
                {
                    var song = PackedRBCONEntry.TryLoadFromCache(in group.ConFile, value.Name, _upgrades, value.Stream, strings);
                    if (song != null)
                    {
                        lock (group.SongEntries)
                        {
                            group.SongEntries.Add(value.Name, song);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private void ReadUnpackedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            var group = ReadExtractedCONGroupHeader(stream);
            if (group == null)
            {
                return;
            }

            var enumerable = new CacheEnumerable<(string Name, UnmanagedMemoryStream Stream)?>(stream, tracker, () =>
            {
                string name = stream.ReadString();
                int length = stream.Read<int>(Endianness.Little);
                if (invalidSongsInCache.Contains(name))
                {
                    stream.Position += length;
                    return null;
                }
                return (name, stream.Slice(length));
            });

            Parallel.ForEach(enumerable, slice =>
            {
                var value = slice!.Value;
                // Error catching must be done per-thread
                try
                {
                    var song = UnpackedRBCONEntry.TryLoadFromCache(group.Location, group.DTAInfo, value.Name, _upgrades, value.Stream, strings);
                    if (song != null)
                    {
                        lock (group.SongEntries)
                        {
                            group.SongEntries.Add(value.Name, song);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private void QuickReadIniGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            string directory = stream.ReadString();
            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(stream, tracker, () =>
            {
                int length = stream.Read<int>(Endianness.Little);
                return stream.Slice(length);
            });

            Parallel.ForEach(enumerable, slice =>
            {
                try
                {
                    QuickReadIniEntry(directory, slice, strings);
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private void QuickReadCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            var group = QuickReadCONGroupHeader(stream);
            if (group == null)
                return;

            var enumerable = new CacheEnumerable<(string Name, UnmanagedMemoryStream Stream)>(stream, tracker, () =>
            {
                string name = stream.ReadString();
                int length = stream.Read<int>(Endianness.Little);
                return (name, stream.Slice(length));
            });

            Parallel.ForEach(enumerable, slice =>
            {
                try
                {
                    AddEntry(PackedRBCONEntry.LoadFromCache_Quick(in group.ConFile, slice.Name, _upgrades, slice.Stream, strings));
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private void QuickReadExtractedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            string directory = stream.ReadString();
            var dta = new AbridgedFileInfo(Path.Combine(directory, "songs.dta"), stream);

            var enumerable = new CacheEnumerable<(string Name, UnmanagedMemoryStream Stream)>(stream, tracker, () =>
            {
                string name = stream.ReadString();
                int length = stream.Read<int>(Endianness.Little);
                return (name, stream.Slice(length));
            });

            Parallel.ForEach(enumerable, slice =>
            {
                try
                {
                    AddEntry(UnpackedRBCONEntry.LoadFromCache_Quick(directory, dta, slice.Name, _upgrades, slice.Stream, strings));
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private static void AddParallelCONTasks(UnmanagedMemoryStream stream, List<Task> conTasks, Action<UnmanagedMemoryStream> func, ParallelExceptionTracker tracker)
        {
            int sectionLength = stream.Read<int>(Endianness.Little);
            var sectionSlice = stream.Slice(sectionLength);
            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(sectionSlice, tracker, () =>
            {
                int length = sectionSlice.Read<int>(Endianness.Little);
                return sectionSlice.Slice(length);
            });

            conTasks.Add(Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(enumerable, groupSlice =>
                {
                    try
                    {
                        func(groupSlice);
                    }
                    catch (Exception ex)
                    {
                        tracker.Set(ex);
                    }
                });
            }, TaskCreationOptions.LongRunning));
        }

        private static void AddParallelEntryTasks(UnmanagedMemoryStream stream, List<Task> entryTasks, CategoryCacheStrings strings, Action<UnmanagedMemoryStream, CategoryCacheStrings, ParallelExceptionTracker> func, ParallelExceptionTracker tracker)
        {
            int sectionLength = stream.Read<int>(Endianness.Little);
            var sectionSlice = stream.Slice(sectionLength);
            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(sectionSlice, tracker, () =>
            {
                int length = sectionSlice.Read<int>(Endianness.Little);
                return sectionSlice.Slice(length);
            });

            entryTasks.Add(Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(enumerable, groupSlice =>
                {
                    try
                    {
                        func(groupSlice, strings, tracker);
                    }
                    catch (Exception ex)
                    {
                        tracker.Set(ex);
                    }
                });
            }, TaskCreationOptions.LongRunning));
        }
    }
}
