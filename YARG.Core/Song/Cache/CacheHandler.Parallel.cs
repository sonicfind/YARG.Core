using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using YARG.Core.Extensions;
using YARG.Core.IO;
using YARG.Core.Logging;
using YARG.Core.Utility;

namespace YARG.Core.Song.Cache
{
    internal sealed class ParallelCacheHandler : CacheHandler
    {
        internal sealed class YARGThreadPool
        {
            private readonly Thread[] _threads = new Thread[Environment.ProcessorCount];
            private readonly Queue<Action> _queue = new();
            private readonly object _lock = new();
            private bool _stop;
            private int _count = 0;

            public YARGThreadPool()
            {
                for (int i = 0; i < _threads.Length; ++i)
                {
                    _threads[i] = new Thread(Run);
                    _threads[i].Start();
                }
            }

            public void AddAction(Action action)
            {
                if (_stop)
                {
                    return;
                }

                lock (_lock)
                {
                    ++_count;
                }

                lock (_queue)
                {
                    _queue.Enqueue(action);
                    Monitor.Pulse(_queue);
                }
            }

            public void AddActions<T>(IEnumerable<T> enumerable, Action<T> action)
            {
                foreach (var item in enumerable)
                {
                    AddAction(() => action(item));
                }
            }

            public void AddActions(int startIndex, int endIndex, Action<int> action)
            {
                while (startIndex < endIndex)
                {
                    int i = startIndex++;
                    AddAction(() => action(i));
                }
            }

            public void Wait()
            {
                while (true)
                {
                    lock (_lock)
                    {
                        if (_count == 0)
                        {
                            return;
                        }
                        Monitor.Wait(_lock);
                    }
                }
            }

            private void Run()
            {
                while (true)
                {
                    Action action;
                    lock (_queue)
                    {
                        if (_stop)
                        {
                            break;
                        }

                        if (_queue.Count == 0)
                        {
                            Monitor.Wait(_queue);
                            continue;
                        }
                        action = _queue.Dequeue();
                    }

                    action();
                    lock (_lock)
                    {
                        --_count;
                        Monitor.Pulse(_lock);
                    }
                }
            }

            ~YARGThreadPool()
            {
                lock (_queue)
                {
                    _stop = true;
                    Monitor.PulseAll(_queue);
                }

                for (int i = 0; i < _threads.Length; i++)
                {
                    _threads[i].Join();
                    _threads[i] = null!;
                }
                GC.SuppressFinalize(this);
            }
        }

        private sealed class ParallelExceptionTracker : Exception
        {
            private readonly object _lock = new();
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

        private sealed class Counter
        {
            private int _count = 0;
            public void Add(int value)
            {
                lock (this)
                {
                    _count += value;
                }
            }

            public void Decrement()
            {
                lock (this)
                {
                    --_count;
                    Monitor.Pulse(this);
                }
            }

            public void Wait()
            {
                while (true)
                {
                    lock (this)
                    {
                        if (_count == 0)
                        {
                            return;
                        }
                        Monitor.Wait(this);
                    }
                }
            }
        }

        private static readonly YARGThreadPool _threadPool = new();

        internal ParallelCacheHandler(List<string> baseDirectories, bool allowDuplicates, bool fullDirectoryPlaylists)
            : base(baseDirectories, allowDuplicates, fullDirectoryPlaylists) { }

        protected override void FindNewEntries()
        {
            var tracker = new PlaylistTracker(fullDirectoryPlaylists, null);
            _threadPool.AddActions(iniGroups, group =>
            {
                var dirInfo = new DirectoryInfo(group.Directory);
                ScanDirectory(dirInfo, group, tracker);
            });
            _threadPool.Wait();

            _threadPool.AddActions(conGroups, group =>
            {
                if (group.LoadSongs(out var container))
                {
                    ScanCONGroup(group, ref container, ScanPackedCONNode);
                }

                lock (group)
                {
                    group.DisposeSongDTA();
                }
            });

            _threadPool.AddActions(extractedConGroups, group =>
            {
                if (group.LoadDTA(out var container))
                {
                    ScanCONGroup(group, ref container, ScanUnpackedCONNode);
                }

                lock (group)
                {
                    group.DisposeSongDTA();
                }
            });
            _threadPool.Wait();

            foreach (var group in conGroups)
            {
                group.DisposeUpgradeDTA();
            }
        }

        protected override void TraverseDirectory(in FileCollection collection, IniGroup group, PlaylistTracker tracker)
        {
            _threadPool.AddActions(collection.SubDirectories, directory => ScanDirectory(directory.Value, group, tracker));
            _threadPool.AddActions(collection.Subfiles,            file => ScanFile(file.Value, group, in tracker));
        }

        protected override bool AddEntry(SongEntry entry)
        {
            lock (cache.Entries)
            {
                return base.AddEntry(entry);
            }
        }

        protected override void SortEntries()
        {
            _threadPool.AddActions(cache.Entries, node =>
            {
                foreach (var entry in node.Value)
                {
                    CategorySorter<string, TitleConfig>.Add(entry, cache.Titles);
                    CategorySorter<SortString, ArtistConfig>.Add(entry, cache.Artists);
                    CategorySorter<SortString, AlbumConfig>.Add(entry, cache.Albums);
                    CategorySorter<SortString, GenreConfig>.Add(entry, cache.Genres);
                    CategorySorter<string, YearConfig>.Add(entry, cache.Years);
                    CategorySorter<SortString, CharterConfig>.Add(entry, cache.Charters);
                    CategorySorter<SortString, PlaylistConfig>.Add(entry, cache.Playlists);
                    CategorySorter<SortString, SourceConfig>.Add(entry, cache.Sources);
                    CategorySorter<string, ArtistAlbumConfig>.Add(entry, cache.ArtistAlbums);
                    CategorySorter<string, SongLengthConfig>.Add(entry, cache.SongLengths);
                    CategorySorter<DateTime, DateAddedConfig>.Add(entry, cache.DatesAdded);
                    InstrumentSorter.Add(entry, cache.Instruments);
                }
            });
            _threadPool.Wait();
        }

        protected override void Deserialize(UnmanagedMemoryStream stream)
        {
            CategoryCacheStrings strings = new(stream, true);
            var tracker = new ParallelExceptionTracker();
            var counter = new Counter();

            try
            {
                AddParallelEntryTasks(stream, strings, ReadIniGroup, tracker);
                AddParallelCONTasks(stream, ReadUpdateDirectory, tracker, counter);
                AddParallelCONTasks(stream, ReadUpgradeDirectory, tracker, counter);
                AddParallelCONTasks(stream, ReadUpgradeCON, tracker, counter);
                counter.Wait();

                AddParallelEntryTasks(stream, strings, ReadPackedCONGroup, tracker);
                AddParallelEntryTasks(stream, strings, ReadUnpackedCONGroup, tracker);
            }
            catch (Exception ex)
            {
                tracker.Set(ex);
                counter.Wait();
            }
            _threadPool.Wait();

            if (tracker.IsSet())
                throw tracker;
        }

        protected override void Deserialize_Quick(UnmanagedMemoryStream stream)
        {
            YargLogger.LogDebug("Quick Read start");
            CategoryCacheStrings strings = new(stream, true);
            var tracker = new ParallelExceptionTracker();
            var counter = new Counter();

            try
            {
                AddParallelEntryTasks(stream, strings, QuickReadIniGroup, tracker);

                int skipLength = stream.Read<int>(Endianness.Little);
                stream.Position += skipLength;

                AddParallelCONTasks(stream, QuickReadUpgradeDirectory, tracker, counter);
                AddParallelCONTasks(stream, QuickReadUpgradeCON, tracker, counter);
                counter.Wait();

                AddParallelEntryTasks(stream, strings, QuickReadCONGroup, tracker);
                AddParallelEntryTasks(stream, strings, QuickReadExtractedCONGroup, tracker);
            }
            catch (Exception ex)
            {
                tracker.Set(ex);
                counter.Wait();
            }
            _threadPool.Wait();

            if (tracker.IsSet())
            {
                throw tracker;
            }
        }

        protected override void AddUpdate(string name, DateTime dtaLastWrite, SongUpdate update)
        {
            lock (updates)
            {
                if (!updates.TryGetValue(name, out var list))
                {
                    updates.Add(name, list = new());
                }
                list.Add(dtaLastWrite, update);
            }
        }

        protected override void AddUpgrade(string name, in YARGTextContainer<byte> container, RBProUpgrade upgrade)
        {
            lock (upgrades)
            {
                upgrades[name] = new(container, upgrade);
            }
        }

        protected override void AddPackedCONGroup(PackedCONGroup group)
        {
            lock (conGroups)
            {
                conGroups.Add(group);
            }
        }

        protected override void AddUnpackedCONGroup(UnpackedCONGroup group)
        {
            lock (extractedConGroups)
            {
                extractedConGroups.Add(group);
            }
        }

        protected override void AddUpdateGroup(UpdateGroup group)
        {
            lock (updateGroups)
            {
                updateGroups.Add(group);
            }
        }

        protected override void AddUpgradeGroup(UpgradeGroup group)
        {
            lock (upgradeGroups)
            {
                upgradeGroups.Add(group);
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
            lock (conGroups)
            {
                foreach (var group in conGroups)
                {
                    if (group.RemoveEntries(shortname))
                    {
                        YargLogger.LogFormatTrace("{0} - {1} pending rescan", group.Location, item2: shortname);
                    }
                }
            }

            lock (extractedConGroups)
            {
                foreach (var group in extractedConGroups)
                {
                    if (group.RemoveEntries(shortname))
                    {
                        YargLogger.LogFormatTrace("{0} - {1} pending rescan", group.Location, item2: shortname);
                    }
                }
            }
        }

        protected override bool CanAddUpgrade(string shortname, DateTime lastUpdated)
        {
            lock (upgradeGroups)
            {
                return CanAddUpgrade(upgradeGroups, shortname, lastUpdated) ?? false;
            }
        }

        protected override bool CanAddUpgrade_CONInclusive(string shortname, DateTime lastUpdated)
        {
            lock (conGroups)
            {
                var result = CanAddUpgrade(conGroups, shortname, lastUpdated);
                if (result != null)
                {
                    return (bool) result;
                }
            }

            lock (upgradeGroups)
            {
                return CanAddUpgrade(upgradeGroups, shortname, lastUpdated) ?? false;
            }
        }

        protected override bool FindOrMarkDirectory(string directory)
        {
            lock (preScannedDirectories)
            {
                return base.FindOrMarkDirectory(directory);
            }
        }

        protected override bool FindOrMarkFile(string file)
        {
            lock (preScannedFiles)
            {
                return base.FindOrMarkFile(file);
            }
        }

        protected override void AddToBadSongs(string filePath, ScanResult err)
        {
            lock (badSongs)
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
            lock (conGroups)
            {
                return conGroups.Find(node => node.Location == filename);
            }
        }

        private void ScanCONGroup<TGroup>(TGroup group, ref YARGTextContainer<byte> container, Action<TGroup, string, int, YARGTextContainer<byte>> func)
            where TGroup : CONGroup
        {
            try
            {
                Dictionary<string, int> indices = new();
                while (YARGDTAReader.StartNode(ref container))
                {
                    string name = YARGDTAReader.GetNameOfNode(ref container, true);
                    if (indices.TryGetValue(name, out int index))
                    {
                        ++index;
                    }
                    indices[name] = index;

                    lock (group)
                    {
                        group.AddRef();
                    }

                    var node = container;
                    _threadPool.AddAction(() =>
                    {
                        func(group, name, index, node);
                        lock (group)
                        {
                            group.DisposeSongDTA();
                        }
                    });
                    YARGDTAReader.EndNode(ref container);
                }
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, $"Error while scanning CON group {group.Location}!");
            }
        }

        private readonly struct CacheEnumerable<T> : IEnumerable<T>
        {
            private readonly UnmanagedMemoryStream _stream;
            private readonly ParallelExceptionTracker _tracker;
            private readonly Counter? _counter;
            private readonly Func<T?> _creator;

            public CacheEnumerable(UnmanagedMemoryStream stream, ParallelExceptionTracker tracker, Counter? counter, Func<T?> creator)
            {
                _stream = stream;
                _tracker = tracker;
                _counter = counter;
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
                    values._counter?.Add(_count);
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

            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(stream, tracker, null, () =>
            {
                int length = stream.Read<int>(Endianness.Little);
                return stream.Slice(length);
            });

            _threadPool.AddActions(enumerable, slice =>
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
            if (group != null)
            {
                ReadCONGroup(group, stream, strings, tracker);
            }
        }

        private void ReadUnpackedCONGroup(UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
        {
            var group = ReadExtractedCONGroupHeader(stream);
            if (group != null)
            {
                ReadCONGroup(group, stream, strings, tracker);
            }
        }

        private void ReadCONGroup<TGroup>(TGroup group, UnmanagedMemoryStream stream, CategoryCacheStrings strings, ParallelExceptionTracker tracker)
            where TGroup : CONGroup
        {
            var enumerable = new CacheEnumerable<(string Name, int Index, UnmanagedMemoryStream Stream)?>(stream, tracker, null, () =>
            {
                string name = stream.ReadString();
                int index = stream.Read<int>(Endianness.Little);
                int length = stream.Read<int>(Endianness.Little);
                if (invalidSongsInCache.Contains(name))
                {
                    stream.Position += length;
                    return null;
                }
                return (name, index, stream.Slice(length));
            });

            _threadPool.AddActions(enumerable, slice =>
            {
                var value = slice!.Value;
                // Error catching must be done per-thread
                try
                {
                    group.ReadEntry(value.Name, value.Index, upgrades, value.Stream, strings);
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
            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(stream, tracker, null, () =>
            {
                int length = stream.Read<int>(Endianness.Little);
                return stream.Slice(length);
            });

            _threadPool.AddActions(enumerable, slice =>
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

            var enumerable = new CacheEnumerable<(string Name, UnmanagedMemoryStream Stream)>(stream, tracker, null, () =>
            {
                string name = stream.ReadString();
                // index
                stream.Position += 4;

                int length = stream.Read<int>(Endianness.Little);
                return (name, stream.Slice(length));
            });

            _threadPool.AddActions(enumerable, slice =>
            {
                try
                {
                    AddEntry(PackedRBCONEntry.LoadFromCache_Quick(in group.ConFile, slice.Name, upgrades, slice.Stream, strings));
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
            var lastWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            var dta = new AbridgedFileInfo_Length(Path.Combine(directory, "songs.dta"), lastWrite, 0);

            var enumerable = new CacheEnumerable<(string Name, UnmanagedMemoryStream Stream)>(stream, tracker, null, () =>
            {
                string name = stream.ReadString();
                // index
                stream.Position += 4;

                int length = stream.Read<int>(Endianness.Little);
                return (name, stream.Slice(length));
            });

            _threadPool.AddActions(enumerable, slice =>
            {
                try
                {
                    AddEntry(UnpackedRBCONEntry.LoadFromCache_Quick(directory, dta, slice.Name, upgrades, slice.Stream, strings));
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
            });
        }

        private static void AddParallelCONTasks(UnmanagedMemoryStream stream, Action<UnmanagedMemoryStream> func, ParallelExceptionTracker tracker, Counter counter)
        {
            int sectionLength = stream.Read<int>(Endianness.Little);
            var sectionSlice = stream.Slice(sectionLength);
            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(sectionSlice, tracker, counter, () =>
            {
                int length = sectionSlice.Read<int>(Endianness.Little);
                return sectionSlice.Slice(length);
            });

            _threadPool.AddActions(enumerable, groupSlice =>
            {
                try
                {
                    func(groupSlice);
                }
                catch (Exception ex)
                {
                    tracker.Set(ex);
                }
                counter.Decrement();
            });
        }

        private static void AddParallelEntryTasks(UnmanagedMemoryStream stream, CategoryCacheStrings strings, Action<UnmanagedMemoryStream, CategoryCacheStrings, ParallelExceptionTracker> func, ParallelExceptionTracker tracker)
        {
            int sectionLength = stream.Read<int>(Endianness.Little);
            var sectionSlice = stream.Slice(sectionLength);
            var enumerable = new CacheEnumerable<UnmanagedMemoryStream>(sectionSlice, tracker, null, () =>
            {
                int length = sectionSlice.Read<int>(Endianness.Little);
                return sectionSlice.Slice(length);
            });

            _threadPool.AddActions(enumerable, groupSlice =>
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
        }
    }
}
