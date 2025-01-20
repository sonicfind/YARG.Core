using MoonscraperChartEditor.Song.IO;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using YARG.Core.Audio;
using YARG.Core.Chart;
using YARG.Core.Extensions;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Logging;
using YARG.Core.Song.Cache;
using YARG.Core.Venue;

namespace YARG.Core.Song
{
    public sealed class SngEntry : IniSubEntry
    {
        private readonly AbridgedFileInfo _sngInfo;
        private readonly uint _version;

        public override EntryType SubType => EntryType.Sng;
        public override string SortBasedLocation => _sngInfo.FullName;
        public override string ActualLocation => Path.GetDirectoryName(_sngInfo.FullName);
        public override DateTime LastWriteTime => _sngInfo.LastUpdatedTime.Date;

        protected override FixedArray<byte> GetChartData(string filename)
        {
            var data = FixedArray<byte>.Null;
            if (_sngInfo.IsStillValid())
            {
                using var sng = SngFile.TryLoadFromFile(_sngInfo.FullName, false);
                if (sng.IsLoaded)
                {
                    data = sng.LoadAllBytes(filename);
                }
            }
            return data;
        }

        private SngEntry(in AbridgedFileInfo sngInfo, uint version, ChartFormat format)
            : base(format)
        {
            _version = version;
            _sngInfo = sngInfo;
        }

        public override void Serialize(MemoryStream stream, CacheWriteIndices indices)
        {
            // Validation block
            stream.Write(_sngInfo.LastUpdatedTime.ToBinary(), Endianness.Little);
            stream.Write(_version, Endianness.Little);
            stream.WriteByte((byte) _chartFormat);

            // Metadata block
            base.Serialize(stream, indices);
        }

        public override SongChart? LoadChart()
        {
            if (!_sngInfo.IsStillValid())
            {
                return null;
            }

            using var sng = SngFile.TryLoadFromFile(_sngInfo.FullName, false);
            if (!sng.IsLoaded)
            {
                return null;
            }

            var parseSettings = new ParseSettings()
            {
                HopoThreshold = _settings.HopoThreshold,
                SustainCutoffThreshold = _settings.SustainCutoffThreshold,
                StarPowerNote = _settings.OverdiveMidiNote,
                DrumsType = ParseDrumsType(in _parts),
                ChordHopoCancellation = _chartFormat != ChartFormat.Chart
            };

            string file = CHART_FILE_TYPES[(int) _chartFormat].Filename;
            using var stream = sngFile[file].CreateStream(sngFile);
            if (_chartFormat == ChartFormat.Mid || _chartFormat == ChartFormat.Midi)
            {
                return SongChart.FromMidi(in parseSettings, MidFileLoader.LoadMidiFile(stream));
            }

            using var reader = new StreamReader(stream);
            return SongChart.FromDotChart(in parseSettings, reader.ReadToEnd());
        }

        public override StemMixer? LoadAudio(float speed, double volume, params SongStem[] ignoreStems)
        {
            using var sngFile = SngFile.TryLoadFromFile(_sngInfo.FullName, false);
            if (!sngFile.IsLoaded)
            {
                YargLogger.LogFormatError("Failed to load sng file {0}", _sngInfo.FullName);
                return null;
            }

            return CreateAudioMixer(speed, volume, sngFile, ignoreStems);
        }

        public override StemMixer? LoadPreviewAudio(float speed)
        {
            using var sngFile = SngFile.TryLoadFromFile(_sngInfo.FullName, false);
            if (!sngFile.IsLoaded)
            {
                YargLogger.LogFormatError("Failed to load sng file {0}", _sngInfo.FullName);
                return null;
            }

            foreach (var filename in PREVIEW_FILES)
            {
                var stream = sngFile.CreateStream(filename);
                if (stream != null)
                {
                    string fakename = Path.Combine(_sngInfo.FullName, filename);
                    var mixer = GlobalAudioHandler.LoadCustomFile(fakename, stream, speed, 0, SongStem.Preview);
                    if (mixer == null)
                    {
                        stream.Dispose();
                        YargLogger.LogFormatError("Failed to load preview file {0}!", fakename);
                        return null;
                    }
                    return mixer;
                }
            }

            return CreateAudioMixer(speed, 0, sngFile, SongStem.Crowd);
        }

        public override YARGImage? LoadAlbumData()
        {
            using var sngFile = SngFile.TryLoadFromFile(_sngInfo.FullName, false);
            if (!sngFile.IsLoaded)
            {
                return null;
            }

            string filename = _cover;
            var file = sngFile.LoadAllBytes(filename);
            if (!file.IsAllocated)
            {
                foreach (string albumFile in ALBUMART_FILES)
                {
                    file = sngFile.LoadAllBytes(albumFile);
                    if (file.IsAllocated)
                    {
                        filename = albumFile;
                        break;
                    }
                }
            }

            if (file.IsAllocated)
            {
                var image = YARGImage.Load(in file);
                file.Dispose();
                if (image != null)
                {
                    return image;
                }
                YargLogger.LogFormatError("SNG Image mapped to {0} failed to load", filename);
            }
            return null;
        }

        public override BackgroundResult? LoadBackground(BackgroundType options)
        {
            using var sngFile = SngFile.TryLoadFromFile(_sngInfo.FullName, false);
            if (!sngFile.IsLoaded)
            {
                return null;
            }

            if ((options & BackgroundType.Yarground) > 0)
            {
                var stream = sngFile.CreateStream(YARGROUND_FULLNAME);
                if (stream != null)
                {
                    return new BackgroundResult(BackgroundType.Yarground, stream);
                }

                string file = Path.ChangeExtension(_sngInfo.FullName, YARGROUND_EXTENSION);
                if (File.Exists(file))
                {
                    return new BackgroundResult(BackgroundType.Yarground, File.OpenRead(file));
                }
            }

            if ((options & BackgroundType.Video) > 0)
            {
                var stream = sngFile.CreateStream(_video);
                if (stream != null)
                {
                    return new BackgroundResult(BackgroundType.Video, stream);
                }

                foreach (var stem in BACKGROUND_FILENAMES)
                {
                    foreach (var format in VIDEO_EXTENSIONS)
                    {
                        stream = sngFile.CreateStream(stem + format);
                        if (stream != null)
                        {
                            return new BackgroundResult(BackgroundType.Video, stream);
                        }
                    }
                }

                foreach (var format in VIDEO_EXTENSIONS)
                {
                    string file = Path.ChangeExtension(_sngInfo.FullName, format);
                    if (File.Exists(file))
                    {
                        return new BackgroundResult(BackgroundType.Video, File.OpenRead(file));
                    }
                }
            }

            if ((options & BackgroundType.Image) > 0)
            {
                var file = sngFile.LoadAllBytes(_background);
                if (!file.IsAllocated)
                {
                    if (TryGetRandomBackgroundImage(in sngFile, out SngFileListing listing))
                    {
                        file = sngFile.LoadAllBytes(in listing);
                    }
                    else
                    {
                        // Fallback to a potential external image mapped specifically to the sng
                        foreach (var format in IMAGE_EXTENSIONS)
                        {
                            string path = Path.ChangeExtension(_sngInfo.FullName, format);
                            if (File.Exists(path))
                            {
                                file = FixedArray.LoadFile(path);
                                break;
                            }
                        }
                    }
                }

                if (file.IsAllocated)
                {
                    var image = YARGImage.Load(in file);
                    file.Dispose();
                    if (image != null)
                    {
                        return new BackgroundResult(image);
                    }
                }
            }

            return null;
        }

        public override FixedArray<byte> LoadMiloData()
        {
            return FixedArray<byte>.Null;
        }

        private StemMixer? CreateAudioMixer(float speed, double volume, in SngFile sngFile, params SongStem[] ignoreStems)
        {
            bool clampStemVolume = _metadata.Source.ToLowerInvariant() == "yarg";
            var mixer = GlobalAudioHandler.CreateMixer(ToString(), speed, volume, clampStemVolume);
            if (mixer == null)
            {
                YargLogger.LogError("Failed to create mixer");
                return null;
            }

            foreach (var stem in IniAudio.SupportedStems)
            {
                var stemEnum = AudioHelpers.SupportedStems[stem];
                if (ignoreStems.Contains(stemEnum))
                {
                    continue;
                }

                foreach (var format in IniAudio.SupportedFormats)
                {
                    var file = stem + format;
                    var stream = sngFile.CreateStream(file);
                    if (stream != null)
                    {
                        if (mixer.AddChannel(stemEnum, stream))
                        {
                            // No duplicates
                            break;
                        }
                        stream.Dispose();
                        YargLogger.LogFormatError("Failed to load stem file {0}", file);
                    }
                }
            }

            if (mixer.Channels.Count == 0)
            {
                YargLogger.LogError("Failed to add any stems!");
                mixer.Dispose();
                return null;
            }

            if (GlobalAudioHandler.LogMixerStatus)
            {
                YargLogger.LogFormatInfo("Loaded {0} stems", mixer.Channels.Count);
            }
            return mixer;
        }

        public static unsafe (ScanResult, SngEntry?) ProcessNewEntry(FileInfo sngInfo, IniModifierCollection modifiers, uint sngVersion, FixedArray<byte>* file, ChartFormat format, string defaultPlaylist)
        {
            var entry = new SngEntry(new AbridgedFileInfo(sngInfo), sngVersion, format);
            entry._metadata.Playlist = defaultPlaylist;

            var result = ScanChart(entry, file, modifiers);
            return (result, result == ScanResult.Success ? entry : null);
        }

        public static SngEntry? TryLoadFromCache(string filename, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            if (!AbridgedFileInfo.TryParseInfo(filename, stream, out var sngInfo))
            {
                return null;
            }

            uint version = stream.Read<uint>(Endianness.Little);
            if (!SngFile.TryLoadFromFile(sngInfo.FullName, false, out var sngFile) || sngFile.Version != version)
            {
                // TODO: Implement Update-in-place functionality
                return null;
            }

            var format = CHART_FILE_TYPES[stream.ReadByte()].Format;
            var entry = new SngEntry(sngInfo, version, format);
            entry.Deserialize(stream, strings);
            return entry;
        }

        public static SngEntry? LoadFromCache_Quick(string filename, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            var sngInfo = new AbridgedFileInfo(filename, stream);
            uint version = stream.Read<uint>(Endianness.Little);
            var format = CHART_FILE_TYPES[stream.ReadByte()].Format;
            var entry = new SngEntry(sngInfo, version, format);
            entry.Deserialize(stream, strings);
            return entry;
        }
    }
}
