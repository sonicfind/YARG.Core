using System;
using System.IO;
using System.Collections.Generic;
using YARG.Core.Extensions;
using YARG.Core.Song.Cache;
using YARG.Core.IO;
using YARG.Core.Logging;
using YARG.Core.Venue;

namespace YARG.Core.Song
{
    public sealed class UnpackedRBCONEntry : RBCONEntry
    {
        private readonly string _nodename = string.Empty;

        public readonly AbridgedFileInfo? _dta;
        public readonly AbridgedFileInfo? _midi;

        protected override DateTime MidiLastUpdate => _midi!.Value.LastUpdatedTime;
        public override string Location { get; }
        public override string DirectoryActual => Location;
        public override EntryType SubType => EntryType.ExCON;

        public static (ScanResult, UnpackedRBCONEntry?) ProcessNewEntry(UnpackedCONGroup group, string nodename, DTAEntry node, SongUpdate? update, (DTAEntry? entry, RBProUpgrade? node) upgrade)
        {
            var (dtaResult, info) = ProcessDTAs(nodename, node, update, upgrade.entry);
            if (dtaResult != ScanResult.Success)
            {
                return (dtaResult, null);
            }

            if (!info.Location!.StartsWith("songs/" + nodename))
            {
                nodename = info.Location!.Split('/')[1];
            }

            string directory = Path.Combine(group.Location, nodename);
            if (!IsMoggValid(in info.UpdateMogg, directory, nodename))
            {
                return (ScanResult.MoggError, null);
            }
            
            var midiInfo = new FileInfo(Path.Combine(directory, nodename + ".mid"));
            if (!midiInfo.Exists)
            {
                return (ScanResult.MissingMidi, null);
            }

            using var mainMidi = FixedArray<byte>.Load(midiInfo.FullName);
            var (midiResult, hash) = ParseRBCONMidi(in mainMidi, in info.UpdateMidi, upgrade.node, ref info.Parts);
            if (midiResult != ScanResult.Success)
            {
                return (midiResult, null);
            }

            if (info.Metadata.Playlist.Length == 0)
            {
                info.Metadata.Playlist = group.DefaultPlaylist;
            }

            var entry = new UnpackedRBCONEntry(info, upgrade.node, in hash, directory, nodename, midiInfo, in group.DTAInfo);
            return (ScanResult.Success, entry);
        }

        public static UnpackedRBCONEntry? TryLoadFromCache(string directory, in AbridgedFileInfo dta, string nodename, Dictionary<string, (DTAEntry? Entry, RBProUpgrade Upgrade)> upgrades, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            string subname = stream.ReadString();
            string songDirectory = Path.Combine(directory, subname);

            string midiPath = Path.Combine(songDirectory, subname + ".mid");
            var midiInfo = AbridgedFileInfo.TryParseInfo(midiPath, stream);
            if (midiInfo == null)
            {
                return null;
            }

            AbridgedFileInfo? updateMidi = null;
            if (stream.ReadBoolean())
            {
                updateMidi = AbridgedFileInfo.TryParseInfo(stream, false);
                if (updateMidi == null)
                {
                    return null;
                }
            }

            var upgrade = upgrades.TryGetValue(nodename, out var node) ? node.Upgrade : null;
            return new UnpackedRBCONEntry(midiInfo.Value, dta, songDirectory, subname, updateMidi, upgrade, stream, strings);
        }

        public static UnpackedRBCONEntry LoadFromCache_Quick(string directory, in AbridgedFileInfo? dta, string nodename, Dictionary<string, (DTAEntry? Entry, RBProUpgrade Upgrade)> upgrades, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            string subname = stream.ReadString();
            string songDirectory = Path.Combine(directory, subname);

            string midiPath = Path.Combine(songDirectory, subname + ".mid");
            var midiInfo = new AbridgedFileInfo(midiPath, stream);

            AbridgedFileInfo? updateMidi = stream.ReadBoolean() ? new AbridgedFileInfo(stream) : null;

            var upgrade = upgrades.TryGetValue(nodename, out var node) ? node.Upgrade : null;
            return new UnpackedRBCONEntry(midiInfo, dta, songDirectory, subname, updateMidi, upgrade, stream, strings);
        }

        private static bool IsMoggValid(in AbridgedFileInfo? info, string directory, string nodename)
        {
            using var stream = LoadMoggStream(in info, directory, nodename);
            if (stream == null)
            {
                return false;   
            }

            int version = stream.Read<int>(Endianness.Little);
            return version == 0x0A || version == 0xf0;
        }

        private UnpackedRBCONEntry(AbridgedFileInfo midi, AbridgedFileInfo? dta, string directory, string nodename,
            AbridgedFileInfo? updateMidi, RBProUpgrade? upgrade, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
            : base(updateMidi, upgrade, stream, strings)
        {
            Location = directory;

            _midi = midi;
            _dta = dta;
            _nodename = nodename;
        }

        private UnpackedRBCONEntry(in ScanNode info, RBProUpgrade? upgrade, in HashWrapper hash
            , string directory, string nodename, FileInfo midiInfo, in AbridgedFileInfo dtaInfo)
            : base(in info, upgrade, in hash)
        {
            Location = directory;
            _nodename = nodename;
            _midi = new AbridgedFileInfo(midiInfo);
            _dta = dtaInfo;
        }

        public override void Serialize(BinaryWriter writer, CategoryCacheWriteNode node)
        {
            writer.Write(_nodename);
            writer.Write(_midi!.Value.LastUpdatedTime.ToBinary());
            base.Serialize(writer, node);
        }

        public override YARGImage? LoadAlbumData()
        {
            if (_updateImage != null && _updateImage.Value.Exists())
            {
                var update = FixedArray<byte>.Load(_updateImage.Value.FullName);
                return new YARGImage(update);
            }

            string imgFilename = Path.Combine(Location, "gen", _nodename + "_keep.png_xbox");
            return File.Exists(imgFilename) ? new YARGImage(FixedArray<byte>.Load(imgFilename)) : null;
        }

        public override BackgroundResult? LoadBackground(BackgroundType options)
        {
            if ((options & BackgroundType.Yarground) > 0)
            {
                string yarground = Path.Combine(Location, YARGROUND_FULLNAME);
                if (File.Exists(yarground))
                {
                    var stream = File.OpenRead(yarground);
                    return new BackgroundResult(BackgroundType.Yarground, stream);
                }
            }

            if ((options & BackgroundType.Video) > 0)
            {
                foreach (var name in BACKGROUND_FILENAMES)
                {
                    var fileBase = Path.Combine(Location, name);
                    foreach (var ext in VIDEO_EXTENSIONS)
                    {
                        string videoFile = fileBase + ext;
                        if (File.Exists(videoFile))
                        {
                            var stream = File.OpenRead(videoFile);
                            return new BackgroundResult(BackgroundType.Video, stream);
                        }
                    }
                }
            }

            if ((options & BackgroundType.Image) > 0)
            {
                //                                     No "video"
                foreach (var name in BACKGROUND_FILENAMES[..2])
                {
                    var fileBase = Path.Combine(Location, name);
                    foreach (var ext in IMAGE_EXTENSIONS)
                    {
                        var file = new FileInfo(fileBase + ext);
                        if (file.Exists)
                        {
                            var image = YARGImage.Load(file);
                            if (image != null)
                            {
                                return new BackgroundResult(image);
                            }
                        }
                    }
                }
            }
            return null;
        }

        public override FixedArray<byte> LoadMiloData()
        {
            if (_updateMilo != null && _updateMilo.Value.Exists())
            {
                return FixedArray<byte>.Load(_updateMilo.Value.FullName);
            }

            string filename = Path.Combine(Location, "gen", _nodename + ".milo_xbox");
            return File.Exists(filename) ? FixedArray<byte>.Load(filename) : FixedArray<byte>.Null;
        }

        protected override Stream? GetMidiStream()
        {
            if (_dta == null || !_dta.Value.IsStillValid() || !_midi!.Value.IsStillValid())
            {
                return null;
            }
            return new FileStream(_midi.Value.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        protected override Stream? GetMoggStream()
        {
            return LoadMoggStream(in _updateMogg, Location, _nodename);
        }

        private static Stream? LoadMoggStream(in AbridgedFileInfo? updateMogg, string directory, string nodename)
        {
            var stream = LoadUpdateMoggStream(in updateMogg);
            if (stream != null)
            {
                return stream;
            }

            string path = Path.Combine(directory, nodename + ".yarg_mogg");
            if (File.Exists(path))
            {
                return new YargMoggReadStream(path);
            }

            path = Path.Combine(directory, nodename + ".mogg");
            return File.Exists(path) ? File.OpenRead(path) : null;
        }
    }
}
