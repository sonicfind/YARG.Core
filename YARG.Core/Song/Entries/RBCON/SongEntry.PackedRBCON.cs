using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Extensions;
using YARG.Core.Song.Cache;
using YARG.Core.IO;
using YARG.Core.Venue;
using System.Linq;
using YARG.Core.Logging;

namespace YARG.Core.Song
{
    public sealed class PackedRBCONEntry : RBCONEntry
    {
        private readonly CONFileListing? _midiListing;
        private readonly CONFileListing? _moggListing;
        private readonly CONFileListing? _miloListing;
        private readonly CONFileListing? _imgListing;
        private readonly DateTime _lastMidiWrite;

        protected override DateTime MidiLastUpdate => _midiListing?.ConFile.LastUpdatedTime ?? DateTime.MinValue;
        public override string Location { get; }
        public override string DirectoryActual => Path.GetDirectoryName(_midiListing?.ConFile.FullName);
        public override EntryType SubType => EntryType.CON;

        public static (ScanResult, PackedRBCONEntry?) ProcessNewEntry(PackedCONGroup group, string nodename, DTAEntry node, SongUpdate? update, (DTAEntry? entry, RBProUpgrade? node) upgrade)
        {
            var (dtaResult, info) = ProcessDTAs(nodename, node, update, upgrade.entry);
            if (dtaResult != ScanResult.Success)
            {
                return (dtaResult, null);
            }

            group.ConFile.TryGetListing(info.Location + ".mogg", out var moggListing);
            if (!IsMoggValid(in info.UpdateMogg, moggListing, group.Stream))
            {
                return (ScanResult.MoggError, null);
            }

            if (!group.ConFile.TryGetListing(info.Location + ".mid", out var midiListing))
            {
                return (ScanResult.MissingMidi, null);
            }

            using var mainMidi = midiListing.LoadAllBytes(group.Stream);
            var (midiResult, hash) = ParseRBCONMidi(in mainMidi, in info.UpdateMidi, upgrade.node, ref info.Parts);
            if (midiResult != ScanResult.Success)
            {
                return (midiResult, null);
            }

            if (!info.Location!.StartsWith($"songs/{nodename}"))
            {
                nodename = midiListing.Filename.Split('/')[1];
            }

            string genPath = $"songs/{nodename}/gen/{nodename}";
            group.ConFile.TryGetListing(genPath + ".milo_xbox", out var miloListing);
            group.ConFile.TryGetListing(genPath + "_keep.png_xbox", out var imgListing);

            if (info.Metadata.Playlist.Length == 0)
            {
                info.Metadata.Playlist = group.DefaultPlaylist;
            }

            string psuedoDirectory = Path.Combine(group.Location, group.ConFile.GetFilename(midiListing.PathIndex));
            var entry = new PackedRBCONEntry(in info, upgrade.node, in hash, midiListing, moggListing, miloListing, imgListing, psuedoDirectory);
            return (ScanResult.Success, entry);
        }

        public static PackedRBCONEntry? TryLoadFromCache(in CONFile conFile, string nodename, Dictionary<string, (DTAEntry? Entry, RBProUpgrade Upgrade)> upgrades, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var psuedoDirectory = stream.ReadString();

            string midiFilename = stream.ReadString();
            if (!conFile.TryGetListing(midiFilename, out var midiListing))
            {
                return null;
            }

            var lastMidiWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
            if (midiListing.LastWrite != lastMidiWrite)
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

            conFile.TryGetListing(Path.ChangeExtension(midiFilename, ".mogg"), out var moggListing);

            if (!midiFilename.StartsWith($"songs/{nodename}"))
                nodename = midiFilename.Split('/')[1];

            string genPath = $"songs/{nodename}/gen/{nodename}";
            conFile.TryGetListing(genPath + ".milo_xbox", out var miloListing);
            conFile.TryGetListing(genPath + "_keep.png_xbox", out var imgListing);
            return new PackedRBCONEntry(midiListing, lastMidiWrite, moggListing, miloListing, imgListing, psuedoDirectory, updateMidi, upgrade, stream, strings);
        }

        public static PackedRBCONEntry LoadFromCache_Quick(in CONFile conFile, string nodename, Dictionary<string, (DTAEntry? Entry, RBProUpgrade Upgrade)> upgrades, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var psuedoDirectory = stream.ReadString();

            string midiFilename = stream.ReadString();
            conFile.TryGetListing(midiFilename, out var midiListing);
            var lastMidiWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));

            AbridgedFileInfo? updateMidi = stream.ReadBoolean() ? new AbridgedFileInfo(stream) : null;
            var upgrade = upgrades.TryGetValue(nodename, out var node) ? node.Upgrade : null;

            conFile.TryGetListing(Path.ChangeExtension(midiFilename, ".mogg"), out var moggListing);

            if (!midiFilename.StartsWith($"songs/{nodename}"))
                nodename = midiFilename.Split('/')[1];

            string genPath = $"songs/{nodename}/gen/{nodename}";
            conFile.TryGetListing(genPath + ".milo_xbox", out var miloListing);
            conFile.TryGetListing(genPath + "_keep.png_xbox", out var imgListing);
            return new PackedRBCONEntry(midiListing, lastMidiWrite, moggListing, miloListing, imgListing, psuedoDirectory, updateMidi, upgrade, stream, strings);
        }

        private static bool IsMoggValid(in AbridgedFileInfo? info, CONFileListing? listing, FileStream stream)
        {
            using var mogg = LoadUpdateMoggStream(in info);
            if (mogg != null)
            {
                int version = mogg.Read<int>(Endianness.Little);
                return version == 0x0A || version == 0xf0;
            }
            return listing != null && CONFileListing.GetMoggVersion(listing, stream) == 0x0A;
        }

        private PackedRBCONEntry(in ScanNode info, RBProUpgrade? upgrade, in HashWrapper hash
            , CONFileListing midiListing, CONFileListing? moggListing, CONFileListing? miloListing, CONFileListing? imgListing, string psuedoDirectory)
            : base(in info, upgrade, in hash)
        {
            _midiListing = midiListing;
            _lastMidiWrite = midiListing.LastWrite;

            _moggListing = moggListing;
            _miloListing = miloListing;
            _imgListing = imgListing;
            Location = psuedoDirectory;
        }

        private PackedRBCONEntry(CONFileListing? midi, DateTime midiLastWrite, CONFileListing? moggListing, CONFileListing? miloListing, CONFileListing? imgListing, string directory,
            AbridgedFileInfo? updateMidi, RBProUpgrade? upgrade, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
            : base(updateMidi, upgrade, stream, strings)
        {
            _midiListing = midi;
            _moggListing = moggListing;
            _miloListing = miloListing;
            _imgListing = imgListing;
            _lastMidiWrite = midiLastWrite;

            Location = directory;
        }

        public override void Serialize(BinaryWriter writer, CategoryCacheWriteNode node)
        {
            writer.Write(Location);
            writer.Write(_midiListing!.Filename);
            writer.Write(_midiListing.LastWrite.ToBinary());
            base.Serialize(writer, node);
        }

        public override YARGImage? LoadAlbumData()
        {
            var bytes = FixedArray<byte>.Null;
            if (_updateImage != null && _updateImage.Value.Exists())
            {
                bytes = FixedArray<byte>.Load(_updateImage.Value.FullName);
            }
            else if (_imgListing != null)
            {
                bytes = _imgListing.LoadAllBytes();
            }
            return bytes.IsAllocated ? new YARGImage(bytes) : null;
        }

        public override BackgroundResult? LoadBackground(BackgroundType options)
        {
            if (_midiListing == null)
            {
                return null;
            }

            string actualDirectory = Path.GetDirectoryName(_midiListing.ConFile.FullName);
            string conName = Path.GetFileNameWithoutExtension(_midiListing.ConFile.FullName);
            string nodename = _midiListing.Filename.Split('/')[1];
            if ((options & BackgroundType.Yarground) > 0)
            {
                string specifcVenue = Path.Combine(actualDirectory, nodename + YARGROUND_EXTENSION);
                if (File.Exists(specifcVenue))
                {
                    var stream = File.OpenRead(specifcVenue);
                    return new BackgroundResult(BackgroundType.Yarground, stream);
                }

                specifcVenue = Path.Combine(actualDirectory, conName + YARGROUND_EXTENSION);
                if (File.Exists(specifcVenue))
                {
                    var stream = File.OpenRead(specifcVenue);
                    return new BackgroundResult(BackgroundType.Yarground, stream);
                }

                var venues = System.IO.Directory.EnumerateFiles(actualDirectory)
                    .Where(file => Path.GetExtension(file) == YARGROUND_EXTENSION)
                    .ToArray();

                if (venues.Length > 0)
                {
                    var stream = File.OpenRead(venues[BACKROUND_RNG.Next(venues.Length)]);
                    return new BackgroundResult(BackgroundType.Yarground, stream);
                }
            }

            if ((options & BackgroundType.Video) > 0)
            {
                string[] filenames = { nodename, conName, "bg", "background", "video" };
                foreach (var name in filenames)
                {
                    string fileBase = Path.Combine(actualDirectory, name);
                    foreach (var ext in VIDEO_EXTENSIONS)
                    {
                        string backgroundPath = fileBase + ext;
                        if (File.Exists(backgroundPath))
                        {
                            var stream = File.OpenRead(backgroundPath);
                            return new BackgroundResult(BackgroundType.Video, stream);
                        }
                    }
                }
            }

            if ((options & BackgroundType.Image) > 0)
            {
                string[] filenames = { nodename, conName, "bg", "background" };
                foreach (var name in filenames)
                {
                    var fileBase = Path.Combine(actualDirectory, name);
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
            return _miloListing != null ? _miloListing.LoadAllBytes() : FixedArray<byte>.Null;
        }

        protected override Stream? GetMidiStream()
        {
            return _midiListing != null && _midiListing.IsStillValid(_lastMidiWrite)
                ? _midiListing.CreateStream()
                : null;
        }

        protected override Stream? GetMoggStream()
        {
            return LoadUpdateMoggStream(in _updateMogg) ?? _moggListing?.CreateStream();
        }
    }
}
