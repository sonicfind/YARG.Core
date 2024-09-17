using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Audio;
using YARG.Core.Extensions;
using YARG.Core.IO;
using YARG.Core.Logging;

namespace YARG.Core.Song.Cache
{
    public enum ScanStage
    {
        LoadingCache,
        LoadingSongs,
        CleaningDuplicates,
        Sorting,
        WritingCache,
        WritingBadSongs
    }

    public struct ScanProgressTracker
    {
        public ScanStage Stage;
        public int Count;
        public int NumScannedDirectories;
        public int BadSongCount;
    }

    public abstract class CacheHandler
    {
        /// <summary>
        /// The date revision of the cache format, relative to UTC.
        /// Format is YY_MM_DD_RR: Y = year, M = month, D = day, R = revision (reset across dates, only increment
        /// if multiple cache version changes happen in a single day).
        /// </summary>
        public const int CACHE_VERSION = 24_09_22_01;

        public static ScanProgressTracker Progress => _progress;
        private static ScanProgressTracker _progress;

        public static SongCache RunScan(bool fast, string cacheLocation, string badSongsLocation, bool multithreading, bool allowDuplicates, bool fullDirectoryPlaylists, List<string> baseDirectories)
        {
            CacheHandler handler = multithreading
                ? new ParallelCacheHandler(baseDirectories, allowDuplicates, fullDirectoryPlaylists)
                : new SequentialCacheHandler(baseDirectories, allowDuplicates, fullDirectoryPlaylists);

            GlobalAudioHandler.LogMixerStatus = false;
            try
            {
                if (!fast || !QuickScan(handler, cacheLocation))
                    FullScan(handler, !fast, cacheLocation, badSongsLocation);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Unknown error while running song scan!");
            }
            GlobalAudioHandler.LogMixerStatus = true;
            return handler._cache;
        }

        private static bool QuickScan(CacheHandler handler, string cacheLocation)
        {
            try
            {
                using var cacheFile = LoadCacheToMemory(cacheLocation, handler._fullDirectoryPlaylists);
                if (cacheFile.IsAllocated)
                {
                    _progress.Stage = ScanStage.LoadingCache;
                    YargLogger.LogDebug("Quick Read start");
                    handler.Deserialize_Quick(cacheFile.ToStream());
                }
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Error occurred during quick cache file read!");
            }

            if (_progress.Count == 0)
            {
                return false;
            }

            handler.CleanupDuplicates();

            _progress.Stage = ScanStage.Sorting;
            handler.SortEntries();
            YargLogger.LogFormatDebug("Total Entries: {0}", _progress.Count);
            return true;
        }

        private static void FullScan(CacheHandler handler, bool loadCache, string cacheLocation, string badSongsLocation)
        {
            if (loadCache)
            {
                try
                {
                    using var cacheFile = LoadCacheToMemory(cacheLocation, handler._fullDirectoryPlaylists);
                    if (cacheFile.IsAllocated)
                    {
                        _progress.Stage = ScanStage.LoadingCache;
                        YargLogger.LogDebug("Full Read start");
                        handler.Deserialize(cacheFile.ToStream());
                    }
                }
                catch (Exception ex)
                {
                    YargLogger.LogException(ex, "Error occurred during full cache file read!");
                }
            }

            _progress.Stage = ScanStage.LoadingSongs;
            handler.FindNewEntries();
            _progress.Stage = ScanStage.CleaningDuplicates;
            handler.CleanupDuplicates();

            _progress.Stage = ScanStage.Sorting;
            handler.SortEntries();
            YargLogger.LogFormatDebug("Total Entries: {0}", _progress.Count);

            try
            {
                _progress.Stage = ScanStage.WritingCache;
                handler.Serialize(cacheLocation);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Error when writing song cache!");
            }

            try
            {
                if (handler._badSongs.Count > 0)
                {
                    _progress.Stage = ScanStage.WritingBadSongs;
                    handler.WriteBadSongs(badSongsLocation);
                }
                else
                {
                    File.Delete(badSongsLocation);
                }
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Error when writing bad songs file!");
            }
        }

        #region Data

        protected readonly SongCache _cache = new();

        protected readonly List<IniGroup> _iniGroups;
        protected readonly List<UpdateGroup> _updateGroups = new();
        protected readonly List<UpgradeGroup> _upgradeGroups = new();
        protected readonly List<PackedCONGroup> _conGroups = new();
        protected readonly List<UnpackedCONGroup> _extractedConGroups = new();

        protected readonly Dictionary<string, SongUpdate> _updates = new();
        protected readonly Dictionary<string, (DTAEntry? entry, RBProUpgrade Upgrade)> _upgrades = new();
        protected readonly HashSet<string> _preScannedDirectories = new();
        protected readonly HashSet<string> _preScannedFiles = new();


        protected readonly bool _allowDuplicates = true;
        protected readonly bool _fullDirectoryPlaylists;
        protected readonly List<SongEntry> _duplicatesRejected = new();
        protected readonly List<SongEntry> _duplicatesToRemove = new();
        protected readonly SortedDictionary<string, ScanResult> _badSongs = new();
        #endregion

        #region Common

        protected CacheHandler(List<string> baseDirectories, bool allowDuplicates, bool fullDirectoryPlaylists)
        {
            _progress = default;
            _allowDuplicates = allowDuplicates;
            _fullDirectoryPlaylists = fullDirectoryPlaylists;

            _iniGroups = new(baseDirectories.Count);
            foreach (string dir in baseDirectories)
            {
                if (!string.IsNullOrEmpty(dir) && !_iniGroups.Exists(group => { return group.Directory == dir; }))
                {
                    _iniGroups.Add(new IniGroup(dir));
                }
            }
        }

        protected abstract void SortEntries();
        protected abstract void AddUpdate(string name, SongUpdate update);
        protected abstract void AddUpgrade(string name, DTAEntry? entry, RBProUpgrade upgrade);
        protected abstract void AddPackedCONGroup(PackedCONGroup group);
        protected abstract void AddUnpackedCONGroup(UnpackedCONGroup group);
        protected abstract void AddUpdateGroup(UpdateGroup group);
        protected abstract void AddUpgradeGroup(UpgradeGroup group);
        protected abstract void RemoveCONEntry(string shortname);
        protected abstract bool CanAddUpgrade(string shortname, DateTime lastUpdated);
        protected abstract bool CanAddUpgrade_CONInclusive(string shortname, DateTime lastUpdated);
        protected abstract Dictionary<string, Dictionary<string, FileInfo>> MapUpdateFiles(in FileCollection collection);

        protected abstract void FindNewEntries();
        protected abstract void TraverseDirectory(in FileCollection collection, IniGroup group, PlaylistTracker tracker);

        protected abstract void Deserialize(UnmanagedMemoryStream stream);
        protected abstract void Deserialize_Quick(UnmanagedMemoryStream stream);
        protected abstract void AddCollectionToCache(in FileCollection collection);
        protected abstract PackedCONGroup? FindCONGroup(string filename);

        protected abstract void CleanupDuplicates();

        protected virtual bool FindOrMarkDirectory(string directory)
        {
            if (!_preScannedDirectories.Add(directory))
            {
                return false;
            }
            _progress.NumScannedDirectories++;
            return true;
        }

        protected virtual bool FindOrMarkFile(string file)
        {
            return _preScannedFiles.Add(file);
        }

        protected virtual void AddToBadSongs(string filePath, ScanResult err)
        {
            _badSongs.Add(filePath, err);
            _progress.BadSongCount++;
        }

        protected virtual bool AddEntry(SongEntry entry)
        {
            var hash = entry.Hash;
            if (_cache.Entries.TryGetValue(hash, out var list) && !_allowDuplicates)
            {
                if (list[0].IsPreferedOver(entry))
                {
                    _duplicatesRejected.Add(entry);
                    return false;
                }

                _duplicatesToRemove.Add(list[0]);
                list[0] = entry;
            }
            else
            {
                if (list == null)
                {
                    _cache.Entries.Add(hash, list = new List<SongEntry>());
                }

                list.Add(entry);
                ++_progress.Count;
            }
            return true;
        }

        protected virtual void AddInvalidSong(string name)
        {
            invalidSongsInCache.Add(name);
        }

        protected IniGroup? GetBaseIniGroup(string path)
        {
            foreach (var group in _iniGroups)
            {
                if (path.StartsWith(group.Directory) &&
                    // Ensures directories with similar names (previously separate bases)
                    // that are consolidated in-game to a single base directory
                    // don't have conflicting "relative path" issues
                    (path.Length == group.Directory.Length || path[group.Directory.Length] == Path.DirectorySeparatorChar))
                    return group;
            }
            return null;
        }

        private void WriteBadSongs(string badSongsLocation)
        {
            using var stream = new FileStream(badSongsLocation, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);

            writer.WriteLine($"Total Errors: {_badSongs.Count}");
            writer.WriteLine();

            foreach (var error in _badSongs)
            {
                writer.WriteLine(error.Key);
                switch (error.Value)
                {
                    case ScanResult.DirectoryError:
                        writer.WriteLine("Error accessing directory contents");
                        break;
                    case ScanResult.DuplicateFilesFound:
                        writer.WriteLine("Multiple sub files or directories that share the same name found in this location.");
                        writer.WriteLine("You must rename or remove all duplicates before they will be processed.");
                        break;
                    case ScanResult.IniEntryCorruption:
                        writer.WriteLine("Corruption of either the ini file or chart/mid file");
                        break;
                    case ScanResult.NoAudio:
                        writer.WriteLine("No audio accompanying the chart file");
                        break;
                    case ScanResult.NoName:
                        writer.WriteLine("Name metadata not provided");
                        break;
                    case ScanResult.NoNotes:
                        writer.WriteLine("No notes found");
                        break;
                    case ScanResult.DTAError:
                        writer.WriteLine("Error occured while parsing DTA file node");
                        break;
                    case ScanResult.MoggError:
                        writer.WriteLine("Required mogg audio file not present or used invalid encryption");
                        break;
                    case ScanResult.UnsupportedEncryption:
                        writer.WriteLine("Mogg file uses unsupported encryption");
                        break;
                    case ScanResult.MissingMidi:
                        writer.WriteLine("Midi file queried for found missing");
                        break;
                    case ScanResult.MissingUpdateMidi:
                        writer.WriteLine("Update Midi file queried for found missing");
                        break;
                    case ScanResult.MissingUpgradeMidi:
                        writer.WriteLine("Upgrade Midi file queried for found missing");
                        break;
                    case ScanResult.IniNotDownloaded:
                        writer.WriteLine("Ini file not fully downloaded - try again once it completes");
                        break;
                    case ScanResult.ChartNotDownloaded:
                        writer.WriteLine("Chart file not fully downloaded - try again once it completes");
                        break;
                    case ScanResult.PossibleCorruption:
                        writer.WriteLine("Possible corruption of a queried midi file");
                        break;
                    case ScanResult.FailedSngLoad:
                        writer.WriteLine("File structure invalid or corrupted");
                        break;
                    case ScanResult.PathTooLong:
                        writer.WriteLine("Path too long for the Windows Filesystem (path limitation can be changed in registry settings if you so wish)");
                        break;
                    case ScanResult.MultipleMidiTrackNames:
                        writer.WriteLine("At least one track fails midi spec for containing multiple unique track names (thus making it ambiguous)");
                        break;
                    case ScanResult.MultipleMidiTrackNames_Update:
                        writer.WriteLine("At least one track fails midi spec for containing multiple unique track names (thus making it ambiguous) - Thrown by a midi update");
                        break;
                    case ScanResult.MultipleMidiTrackNames_Upgrade:
                        writer.WriteLine("At least one track fails midi spec for containing multiple unique track names (thus making it ambiguous) - Thrown by a pro guitar upgrade");
                        break;
                    case ScanResult.LooseChart_Warning:
                        writer.WriteLine("Loose chart files halted all traversal into the subdirectories at this location.");
                        writer.WriteLine("To fix, if desired, place the loose chart files in a separate dedicated folder.");
                        break;
                }
                writer.WriteLine();
            }
        }

        protected static bool TryRemove<TGroup, TEntry>(List<TGroup> groups, SongEntry entry)
            where TGroup : ICacheGroup<TEntry>
            where TEntry : SongEntry
        {
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.TryRemoveEntry(entry))
                {
                    if (group.Count == 0)
                    {
                        groups.RemoveAt(i);
                    }
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Scanning

        protected readonly struct PlaylistTracker
        {
            private readonly bool _fullDirectoryFlag;
            private readonly string? _playlist;

            public string Playlist => !string.IsNullOrEmpty(_playlist) ? _playlist : "Unknown Playlist";

            public PlaylistTracker(bool fullDirectoryFlag, string? playlist)
            {
                _fullDirectoryFlag = fullDirectoryFlag;
                _playlist = playlist;
            }

            public PlaylistTracker Append(string directory)
            {
                string playlist = string.Empty;
                if (_playlist != null)
                {
                    playlist = _fullDirectoryFlag ? Path.Combine(_playlist, directory) : directory;
                }
                return new PlaylistTracker(_fullDirectoryFlag, playlist);
            }
        }

        protected const string SONGS_DTA = "songs.dta";
        protected const string SONGUPDATES_DTA = "songs_updates.dta";
        protected const string SONGUPGRADES_DTA = "upgrades.dta";

        protected void ScanDirectory(DirectoryInfo directory, IniGroup group, PlaylistTracker tracker)
        {
            try
            {
                if (!FindOrMarkDirectory(directory.FullName) || (directory.Attributes & FileAttributes.Hidden) != 0)
                {
                    return;
                }

                if (!collectionCache.TryGetValue(directory.FullName, out var collection))
                {
                    collection = new FileCollection(directory);
                }

                if (ScanIniEntry(in collection, group, tracker.Playlist))
                {
                    if (collection.SubDirectories.Count > 0)
                    {
                        AddToBadSongs(directory.FullName, ScanResult.LooseChart_Warning);
                    }
                    return;
                }

                switch (directory.Name)
                {
                    case "songs_updates":
                        {
                            if (collection.Subfiles.TryGetValue(SONGUPDATES_DTA, out var dta))
                            {
                                var mapping = MapUpdateFiles(in collection);
                                var updateGroup = new UpdateGroup(in collection, dta, mapping);
                                foreach(var entry in updateGroup.Updates)
                                {
                                    AddUpdate(entry.Key, entry.Value);
                                    RemoveCONEntry(entry.Key);
                                }
                                AddUpdateGroup(updateGroup);
                                return;
                            }
                            break;
                        }
                    case "songs_upgrades":
                        {
                            if (collection.Subfiles.TryGetValue(SONGUPGRADES_DTA, out var dta))
                            {
                                var upgradeGroup = new UpgradeGroup(in collection, dta);
                                foreach (var entry in upgradeGroup.Upgrades)
                                {
                                    if (entry.Value.Upgrade != null && CanAddUpgrade(entry.Key, entry.Value.Upgrade.LastUpdatedTime))
                                    {
                                        AddUpgrade(entry.Key, entry.Value.Entry, entry.Value.Upgrade);
                                        RemoveCONEntry(entry.Key);
                                    }
                                }
                                AddUpgradeGroup(upgradeGroup);
                                return;
                            }
                            break;
                        }
                    case "songs":
                        {
                            if (collection.Subfiles.TryGetValue(SONGS_DTA, out var dta))
                            {
                                var exConGroup = new UnpackedCONGroup(directory.FullName, dta, tracker.Playlist);
                                AddUnpackedCONGroup(exConGroup);
                                return;
                            }
                            break;
                        }
                }
                TraverseDirectory(collection, group, tracker.Append(directory.Name));
                if (collection.ContainedDupes)
                {
                    AddToBadSongs(collection.Directory.FullName, ScanResult.DuplicateFilesFound);
                }
            }
            catch (PathTooLongException)
            {
                YargLogger.LogFormatError("Path {0} is too long for the file system!", directory.FullName);
                AddToBadSongs(directory.FullName, ScanResult.PathTooLong);
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, $"Error while scanning directory {directory.FullName}!");
            }
        }

        protected void ScanFile(FileInfo info, IniGroup group, in PlaylistTracker tracker)
        {
            string filename = info.FullName;
            try
            {
                // Ensures only fully downloaded unmarked files are processed
                if (FindOrMarkFile(filename) && (info.Attributes & AbridgedFileInfo.RECALL_ON_DATA_ACCESS) == 0)
                {
                    var abridged = new AbridgedFileInfo(info);
                    string ext = info.Extension;
                    if (ext == ".sng" || ext == ".yargsong")
                    {
                        using var sngFile = SngFile.TryLoadFromFile(abridged);
                        if (sngFile != null)
                        {
                            ScanSngFile(sngFile, group, tracker.Playlist);
                        }
                        else
                        {
                            AddToBadSongs(info.FullName, ScanResult.PossibleCorruption);
                        }
                    }
                    else
                    {
                        var confile = CONFile.TryParseListings(abridged);
                        if (confile != null)
                        {
                            var conGroup = new PackedCONGroup(confile.Value, abridged, tracker.Playlist);
                            foreach (var entry in conGroup.Upgrades)
                            {
                                if (entry.Value.Upgrade != null && CanAddUpgrade(entry.Key, entry.Value.Upgrade.LastUpdatedTime))
                                {
                                    AddUpgrade(entry.Key, entry.Value.Entry, entry.Value.Upgrade);
                                    RemoveCONEntry(entry.Key);
                                }
                            }
                            AddPackedCONGroup(conGroup);
                        }
                    }
                }
            }
            catch (PathTooLongException)
            {
                YargLogger.LogFormatError("Path {0} is too long for the file system!", filename);
                AddToBadSongs(filename, ScanResult.PathTooLong);
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, $"Error while scanning file {filename}!");
            }
        }

        private static readonly (string Name, ChartType Type)[] ChartTypes =
        {
            ("notes.mid", ChartType.Mid),
            ("notes.midi", ChartType.Midi),
            ("notes.chart", ChartType.Chart)
        };

        private bool ScanIniEntry(in FileCollection collection, IniGroup group, string defaultPlaylist)
        {
            int i = collection.Subfiles.TryGetValue("song.ini", out var ini) ? 0 : 2;
            while (i < 3)
            {
                if (!collection.Subfiles.TryGetValue(ChartTypes[i].Name, out var chart))
                {
                    ++i;
                    continue;
                }

                if (!collection.ContainsAudio())
                {
                    AddToBadSongs(chart.FullName, ScanResult.NoAudio);
                    break;
                }

                try
                {
                    var node = new IniChartNode<FileInfo>(chart, ChartTypes[i].Type);
                    var entry = UnpackedIniEntry.ProcessNewEntry(collection.Directory.FullName, in node, ini, defaultPlaylist);
                    if (entry.Item2 == null)
                    {
                        AddToBadSongs(chart.FullName, entry.Item1);
                    }
                    else if (AddEntry(entry.Item2))
                    {
                        group.AddEntry(entry.Item2);
                    }
                }
                catch (PathTooLongException)
                {
                    YargLogger.LogFormatError("Path {0} is too long for the file system!", chart);
                    AddToBadSongs(chart.FullName, ScanResult.PathTooLong);
                }
                catch (Exception e)
                {
                    YargLogger.LogException(e, $"Error while scanning chart file {chart}!");
                    AddToBadSongs(collection.Directory.FullName, ScanResult.IniEntryCorruption);
                }
                return true;
            }
            return false;
        }

        private void ScanSngFile(SngFile sngFile, IniGroup group, string defaultPlaylist)
        {
            int i = sngFile.Metadata.Count > 0 ? 0 : 2;
            while (i < 3)
            {
                if (!sngFile.TryGetValue(ChartTypes[i].Name, out var chart))
                {
                    ++i;
                    continue;
                }

                if (!sngFile.ContainsAudio())
                {
                    AddToBadSongs(sngFile.Info.FullName, ScanResult.NoAudio);
                    break;
                }

                try
                {
                    var node = new IniChartNode<SngFileListing>(chart, ChartTypes[i].Type);
                    var entry = SngEntry.ProcessNewEntry(sngFile, in node, defaultPlaylist);
                    if (entry.Item2 == null)
                    {
                        AddToBadSongs(sngFile.Info.FullName, entry.Item1);
                    }
                    else if (AddEntry(entry.Item2))
                    {
                        group.AddEntry(entry.Item2);
                    }
                }
                catch (Exception e)
                {
                    YargLogger.LogException(e, $"Error while scanning chart file {chart} within {sngFile.Info.FullName}!");
                    AddToBadSongs(sngFile.Info.FullName, ScanResult.IniEntryCorruption);
                }
                break;
            }
        }
        #endregion

        #region Serialization

        public const int SIZEOF_DATETIME = 8;
        protected readonly HashSet<string> invalidSongsInCache = new();
        protected readonly Dictionary<string, FileCollection> collectionCache = new();

        /// <summary>
        /// The sum of all "count" variables in a file
        /// 4 - (version number(4 bytes))
        /// 1 - (FullDirectoryPlaylist flag(1 byte))
        /// 64 - (section size(4 bytes) + zero string count(4 bytes)) * # categories(8)
        /// 24 - (# groups(4 bytes) * # group types(6))
        ///
        /// </summary>
        private const int MIN_CACHEFILESIZE = 93;

        private static FixedArray<byte> LoadCacheToMemory(string cacheLocation, bool fullDirectoryPlaylists)
        {
            FileInfo info = new(cacheLocation);
            if (!info.Exists || info.Length < MIN_CACHEFILESIZE)
            {
                YargLogger.LogDebug("Cache invalid or not found");
                return FixedArray<byte>.Null;
            }

            using var stream = new FileStream(cacheLocation, FileMode.Open, FileAccess.Read);
            if (stream.Read<int>(Endianness.Little) != CACHE_VERSION)
            {
                YargLogger.LogDebug($"Cache outdated");
                return FixedArray<byte>.Null;
            }

            if (stream.ReadBoolean() != fullDirectoryPlaylists)
            {
                YargLogger.LogDebug($"FullDirectoryFlag flipped");
                return FixedArray<byte>.Null;
            }
            return FixedArray<byte>.Read(stream, info.Length - stream.Position);
        }

        private void Serialize(string cacheLocation)
        {
            using var writer = new BinaryWriter(new FileStream(cacheLocation, FileMode.Create, FileAccess.Write));
            Dictionary<SongEntry, CategoryCacheWriteNode> nodes = new();

            writer.Write(CACHE_VERSION);
            writer.Write(_fullDirectoryPlaylists);

            CategoryWriter.WriteToCache(writer, _cache.Titles, SongAttribute.Name, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Artists, SongAttribute.Artist, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Albums, SongAttribute.Album, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Genres, SongAttribute.Genre, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Years, SongAttribute.Year, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Charters, SongAttribute.Charter, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Playlists, SongAttribute.Playlist, ref nodes);
            CategoryWriter.WriteToCache(writer, _cache.Sources, SongAttribute.Source, ref nodes);

            List<PackedCONGroup> upgradeCons = new();
            List<PackedCONGroup> entryCons = new();
            foreach (var group in _conGroups)
            {
                if (group.Upgrades.Count > 0)
                    upgradeCons.Add(group);

                if (group.Count > 0)
                    entryCons.Add(group);
            }

            ICacheGroup<IniSubEntry>.SerializeGroups(_iniGroups, writer, nodes);
            IModificationGroup.SerializeGroups(_updateGroups, writer);
            IModificationGroup.SerializeGroups(_upgradeGroups, writer);
            IModificationGroup.SerializeGroups(upgradeCons, writer);
            ICacheGroup<RBCONEntry>.SerializeGroups(entryCons, writer, nodes);
            ICacheGroup<RBCONEntry>.SerializeGroups(_extractedConGroups, writer, nodes);
        }

        protected void ReadIniEntry(IniGroup group, string directory, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            bool isSngEntry = stream.ReadBoolean();
            string fullname = Path.Combine(directory, stream.ReadString());

            IniSubEntry? entry = isSngEntry
                ? ReadSngEntry(fullname, stream, strings)
                : ReadUnpackedIniEntry(fullname, stream, strings);

            if (entry != null && AddEntry(entry))
            {
                group.AddEntry(entry);
            }
        }

        protected void QuickReadIniEntry(string directory, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            bool isSngEntry = stream.ReadBoolean();
            string fullname = Path.Combine(directory, stream.ReadString());

            IniSubEntry? entry = isSngEntry
                ? SngEntry.LoadFromCache_Quick(fullname, stream, strings)
                : UnpackedIniEntry.IniFromCache_Quick(fullname, stream, strings);

            if (entry != null)
            {
                AddEntry(entry);
            }
            else
            {
                YargLogger.LogError("Cache file was modified externally with a bad CHART_TYPE enum value... or bigger error");
            }
        }

        protected void ReadUpdateDirectory(UnmanagedMemoryStream stream)
        {
            string directory = stream.ReadString();
            var dtaLastWritten = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            int count = stream.Read<int>(Endianness.Little);

            // Functions as a "check base directory" call
            if (GetBaseIniGroup(directory) == null)
            {
                goto Invalidate;
            }

            var dirInfo = new DirectoryInfo(directory);
            if (!dirInfo.Exists)
            {
                goto Invalidate;
            }

            var collection = new FileCollection(dirInfo);
            if (!collection.Subfiles.TryGetValue(SONGUPDATES_DTA, out var dta))
            {
                collectionCache.Add(directory, collection);
                goto Invalidate;
            }

            var mapping = MapUpdateFiles(in collection);
            var updateGroup = new UpdateGroup(in collection, dta, mapping);
            foreach (var entry in updateGroup.Updates)
            {
                AddUpdate(entry.Key, entry.Value);
            }
            AddUpdateGroup(updateGroup);
            FindOrMarkDirectory(directory);
            if (updateGroup.DTALastWrite != dtaLastWritten)
            {
                goto Invalidate;
            }

            for (int i = 0; i < count; i++)
            {
                string name = stream.ReadString();
                if (updateGroup.Updates.TryGetValue(name, out var update))
                {
                    if (!update.Validate(stream))
                    {
                        AddInvalidSong(name);
                    }
                }
                else
                {
                    AddInvalidSong(name);
                    SongUpdate.SkipRead(stream);
                }
            }
            return;

        Invalidate:
            for (int i = 0; i < count; i++)
            {
                AddInvalidSong(stream.ReadString());
                SongUpdate.SkipRead(stream);
            }
        }

        protected void ReadUpgradeDirectory(UnmanagedMemoryStream stream)
        {
            string directory = stream.ReadString();
            var dtaLastWritten = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            int count = stream.Read<int>(Endianness.Little);

            // Functions as a "check base directory" call
            if (GetBaseIniGroup(directory) == null)
            {
                goto Invalidate;
            }

            var dirInfo = new DirectoryInfo(directory);
            if (!dirInfo.Exists)
            {
                goto Invalidate;
            }

            var collection = new FileCollection(dirInfo);
            if (!collection.Subfiles.TryGetValue(SONGUPGRADES_DTA, out var dta))
            {
                collectionCache.Add(directory, collection);
                goto Invalidate;
            }

            FindOrMarkDirectory(directory);

            var upgradeGroup = new UpgradeGroup(in collection, dta);
            foreach (var entry in upgradeGroup.Upgrades)
            {
                if (entry.Value.Upgrade != null && CanAddUpgrade(entry.Key, entry.Value.Upgrade.LastUpdatedTime))
                {
                    AddUpgrade(entry.Key, entry.Value.Entry, entry.Value.Upgrade);
                }
            }

            AddUpgradeGroup(upgradeGroup);
            if (dta.LastWriteTime != dtaLastWritten)
            {
                goto Invalidate;
            }

            for (int i = 0; i < count; i++)
            {
                string name = stream.ReadString();
                bool hadUpgradeMidi = stream.ReadBoolean();
                if (!upgradeGroup.Upgrades.TryGetValue(name, out var node) || (hadUpgradeMidi != (node.Upgrade != null)))
                {
                    AddInvalidSong(name);
                    if (hadUpgradeMidi)
                    {
                        stream.Position += 2 * sizeof(long);
                    }
                    continue;
                }

                if (hadUpgradeMidi)
                {
                    var lastUpdated = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
                    // Only need the length value in quick scan
                    stream.Position += sizeof(long);
                    if (node.Upgrade!.LastUpdatedTime != lastUpdated)
                    {
                        AddInvalidSong(name);
                    }
                }
            }
            return;

        Invalidate:
            for (int i = 0; i < count; i++)
            {
                AddInvalidSong(stream.ReadString());
                if (stream.ReadBoolean())
                {
                    stream.Position += 2 * sizeof(long);
                }
            }
        }

        protected void ReadUpgradeCON(UnmanagedMemoryStream stream)
        {
            string filename = stream.ReadString();
            var conLastUpdated = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            var dtaLastWritten = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            int count = stream.Read<int>(Endianness.Little);

            var baseGroup = GetBaseIniGroup(filename);
            if (baseGroup != null)
            {
                // Make playlist as the group is only made once
                string playlist = ConstructPlaylist(filename, baseGroup.Directory);
                var group = CreateCONGroup(filename, playlist);
                if (group == null)
                {
                    goto Invalidate;
                }

                AddPackedCONGroup(group);
                foreach (var entry in group.Upgrades)
                {
                    if (entry.Value.Upgrade != null && CanAddUpgrade(entry.Key, entry.Value.Upgrade.LastUpdatedTime))
                    {
                        AddUpgrade(entry.Key, entry.Value.Entry, entry.Value.Upgrade);
                        RemoveCONEntry(entry.Key);
                    }
                }

                if (group.UpgradeDta!.LastWrite == dtaLastWritten)
                {
                    if (group.Info.LastUpdatedTime != conLastUpdated)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            string name = stream.ReadString();
                            bool hadUpgradeMidi = stream.ReadBoolean();
                            // If no DTAEntry was found
                            if (!group.Upgrades.TryGetValue(name, out var node)
                            // or if the presence of a midi doesn't match the state specified from the cache
                            || (hadUpgradeMidi != (node.Upgrade != null))
                            // or if a present midi has the wrong datetime
                            || (hadUpgradeMidi && node.Upgrade!.LastUpdatedTime != DateTime.FromBinary(stream.Read<long>(Endianness.Little))))
                            {
                                AddInvalidSong(name);
                            }
                        }
                    }
                    return;
                }
            }

        Invalidate:
            for (int i = 0; i < count; i++)
            {
                AddInvalidSong(stream.ReadString());
                stream.Position += SIZEOF_DATETIME;
            }
        }

        protected PackedCONGroup? ReadCONGroupHeader(UnmanagedMemoryStream stream)
        {
            string filename = stream.ReadString();
            var baseGroup = GetBaseIniGroup(filename);
            if (baseGroup == null)
            {
                return null;
            }

            var dtaLastWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            var group = FindCONGroup(filename);
            if (group == null)
            {
                var info = new FileInfo(filename);
                if (!info.Exists)
                {
                    return null;
                }

                FindOrMarkFile(filename);

                var abridged = new AbridgedFileInfo(info);
                var confile = CONFile.TryParseListings(abridged);
                if (confile == null)
                {
                    return null;
                }

                string playlist = ConstructPlaylist(filename, baseGroup.Directory);
                group = new PackedCONGroup(confile.Value, abridged, playlist);
                AddPackedCONGroup(group);
            }

            if (group.SongDTA == null || group.SongDTA.LastWrite != dtaLastWrite)
            {
                return null;
            }
            return group;
        }

        protected UnpackedCONGroup? ReadExtractedCONGroupHeader(UnmanagedMemoryStream stream)
        {
            string directory = stream.ReadString();
            var baseGroup = GetBaseIniGroup(directory);
            if (baseGroup == null)
            {
                return null;
            }

            var dtaInfo = new FileInfo(Path.Combine(directory, "songs.dta"));
            if (!dtaInfo.Exists)
            {
                return null;
            }

            FindOrMarkDirectory(directory);

            string playlist = ConstructPlaylist(directory, baseGroup.Directory);
            var group = new UnpackedCONGroup(directory, dtaInfo, playlist);
            AddUnpackedCONGroup(group);

            if (dtaInfo.LastWriteTime != DateTime.FromBinary(stream.Read<long>(Endianness.Little)))
            {
                return null;
            }
            return group;
        }

        protected void QuickReadUpgradeDirectory(UnmanagedMemoryStream stream)
        {
            string directory = stream.ReadString();
            var dtaLastUpdated = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            int count = stream.Read<int>(Endianness.Little);

            for (int i = 0; i < count; i++)
            {
                string name = stream.ReadString();
                string filename = Path.Combine(directory, $"{name}_plus.mid");
                if (stream.ReadBoolean())
                {
                    var info = new AbridgedFileInfo(filename, stream);
                    AddUpgrade(name, default, new UnpackedRBProUpgrade(info));
                }
            }
        }

        protected void QuickReadUpgradeCON(UnmanagedMemoryStream stream)
        {
            string filename = stream.ReadString();
            stream.Position += 2 * SIZEOF_DATETIME;
            int count = stream.Read<int>(Endianness.Little);

            var group = CreateCONGroup(filename, string.Empty);
            if (group != null)
            {
                AddPackedCONGroup(group);
            }

            for (int i = 0; i < count; i++)
            {
                string name = stream.ReadString();
                if (stream.ReadBoolean())
                {
                    var lastWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
                    var listing = default(CONFileListing);
                    group?.ConFile.TryGetListing($"songs_upgrades/{name}_plus.mid", out listing);

                    var upgrade = new PackedRBProUpgrade(listing, lastWrite);
                    AddUpgrade(name, default, upgrade);
                }
            }
        }

        protected PackedCONGroup? QuickReadCONGroupHeader(UnmanagedMemoryStream stream)
        {
            string filename = stream.ReadString();
            var dtaLastWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));

            var group = FindCONGroup(filename);
            if (group == null)
            {
                group = CreateCONGroup(filename, string.Empty);
                if (group == null)
                {
                    return null;
                }

                AddPackedCONGroup(group);
            }

            if (group.SongDTA == null || group.SongDTA.LastWrite != dtaLastWrite)
            {
                return null;
            }
            return group;
        }

        private UnpackedIniEntry? ReadUnpackedIniEntry(string directory, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var entry = UnpackedIniEntry.TryLoadFromCache(directory, stream, strings);
            if (entry == null)
            {
                return null;
            }

            FindOrMarkDirectory(directory);
            return entry;
        }

        private SngEntry? ReadSngEntry(string fullname, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var entry = SngEntry.TryLoadFromCache(fullname, stream, strings);
            if (entry == null)
            {
                return null;
            }
            FindOrMarkFile(fullname);
            return entry;
        }

        private PackedCONGroup? CreateCONGroup(string filename, string defaultPlaylist)
        {
            var info = new FileInfo(filename);
            if (!info.Exists)
                return null;

            FindOrMarkFile(filename);

            var abridged = new AbridgedFileInfo(info);
            var confile = CONFile.TryParseListings(abridged);
            if (confile == null)
            {
                return null;
            }
            return new PackedCONGroup(confile.Value, abridged, defaultPlaylist);
        }

        private string ConstructPlaylist(string filename, string baseDirectory)
        {
            string directory = Path.GetDirectoryName(filename);
            if (directory.Length == baseDirectory.Length)
            {
                return "Unknown Playlist";
            }

            if (!_fullDirectoryPlaylists)
            {
                return Path.GetFileName(directory);
            }
            return directory[(baseDirectory.Length + 1)..];
        }
        #endregion
    }

    internal static class UnmanagedStreamSlicer
    {
        public static unsafe UnmanagedMemoryStream Slice(this UnmanagedMemoryStream stream, int length)
        {
            var newStream = new UnmanagedMemoryStream(stream.PositionPointer, length);
            stream.Position += length;
            return newStream;
        }
    }

    internal static class AudioFinder
    {
        public static bool ContainsAudio(this SngFile sngFile)
        {
            foreach (var file in sngFile)
            {
                if (IniAudio.IsAudioFile(file.Key))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
