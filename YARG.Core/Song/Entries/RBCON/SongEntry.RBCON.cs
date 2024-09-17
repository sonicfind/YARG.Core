using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Song.Cache;
using YARG.Core.IO;
using YARG.Core.Song.Preparsers;
using Melanchall.DryWetMidi.Core;
using YARG.Core.Extensions;
using YARG.Core.Audio;
using YARG.Core.Logging;

namespace YARG.Core.Song
{
    public abstract class RBCONEntry : SongEntry
    {
        private const long NOTE_SNAP_THRESHOLD = 10;

        private readonly RBMetadata _rbMetadata;
        private readonly RBCONDifficulties _rbDifficulties;

        private readonly AbridgedFileInfo? _updateMidi;
        private readonly RBProUpgrade? _upgrade;

        protected readonly AbridgedFileInfo? _updateMogg;
        protected readonly AbridgedFileInfo? _updateMilo;
        protected readonly AbridgedFileInfo? _updateImage;

        public string RBSongId => _rbMetadata.SongID;
        public int RBBandDiff => _rbDifficulties.Band;

        protected abstract DateTime MidiLastUpdate { get; }

        public override DateTime GetAddDate()
        {
            var lastUpdateTime = MidiLastUpdate;
            if (_updateMidi != null)
            {
                if (_updateMidi.Value.LastUpdatedTime > lastUpdateTime)
                {
                    lastUpdateTime = _updateMidi.Value.LastUpdatedTime;
                }
            }

            if (_upgrade != null)
            {
                if (_upgrade.LastUpdatedTime > lastUpdateTime)
                {
                    lastUpdateTime = _upgrade.LastUpdatedTime;
                }
            }
            return lastUpdateTime.Date;
        }

        public override SongChart? LoadChart()
        {
            MidiFile midi;
            var readingSettings = MidiSettingsLatin1.Instance; // RBCONs are always Latin-1
            // Read base MIDI
            using (var midiStream = GetMidiStream())
            {
                if (midiStream == null)
                    return null;
                midi = MidiFile.Read(midiStream, readingSettings);
            }

            // Merge update MIDI
            if (_updateMidi != null)
            {
                if (!_updateMidi.Value.IsStillValid(false))
                    return null;

                using var midiStream = new FileStream(_updateMidi.Value.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var update = MidiFile.Read(midiStream, readingSettings);
                midi.Merge(update);
            }

            // Merge upgrade MIDI
            if (_upgrade != null)
            {
                using var midiStream = _upgrade.GetUpgradeMidiStream();
                if (midiStream == null)
                    return null;
                var update = MidiFile.Read(midiStream, readingSettings);
                midi.Merge(update);
            }

            return SongChart.FromMidi(_parseSettings, midi);
        }

        public override StemMixer? LoadAudio(float speed, double volume, params SongStem[] ignoreStems)
        {
            var stream = GetMoggStream();
            if (stream == null)
            {
                return null;
            }

            int version = stream.Read<int>(Endianness.Little);
            if (version is not 0x0A and not 0xF0)
            {
                YargLogger.LogError("Original unencrypted mogg replaced by an encrypted mogg!");
                stream.Dispose();
                return null;
            }

            int start = stream.Read<int>(Endianness.Little);
            stream.Seek(start, SeekOrigin.Begin);

            bool clampStemVolume = _metadata.Source.Str.ToLowerInvariant() == "yarg";
            var mixer = GlobalAudioHandler.CreateMixer(ToString(), stream, speed, volume, clampStemVolume);
            if (mixer == null)
            {
                YargLogger.LogError("Mogg failed to load!");
                stream.Dispose();
                return null;
            }


            if (_rbMetadata.Indices.Drums.Length > 0 && !ignoreStems.Contains(SongStem.Drums))
            {
                switch (_rbMetadata.Indices.Drums.Length)
                {
                    //drum (0 1): stereo kit --> (0 1)
                    case 1:
                    case 2:
                        mixer.AddChannel(SongStem.Drums, _rbMetadata.Indices.Drums, _rbMetadata.Panning.Drums!);
                        break;
                    //drum (0 1 2): mono kick, stereo snare/kit --> (0) (1 2)
                    case 3:
                        mixer.AddChannel(SongStem.Drums1, _rbMetadata.Indices.Drums[0..1], _rbMetadata.Panning.Drums![0..2]);
                        mixer.AddChannel(SongStem.Drums2, _rbMetadata.Indices.Drums[1..3], _rbMetadata.Panning.Drums[2..6]);
                        break;
                    //drum (0 1 2 3): mono kick, mono snare, stereo kit --> (0) (1) (2 3)
                    case 4:
                        mixer.AddChannel(SongStem.Drums1, _rbMetadata.Indices.Drums[0..1], _rbMetadata.Panning.Drums![0..2]);
                        mixer.AddChannel(SongStem.Drums2, _rbMetadata.Indices.Drums[1..2], _rbMetadata.Panning.Drums[2..4]);
                        mixer.AddChannel(SongStem.Drums3, _rbMetadata.Indices.Drums[2..4], _rbMetadata.Panning.Drums[4..8]);
                        break;
                    //drum (0 1 2 3 4): mono kick, stereo snare, stereo kit --> (0) (1 2) (3 4)
                    case 5:
                        mixer.AddChannel(SongStem.Drums1, _rbMetadata.Indices.Drums[0..1], _rbMetadata.Panning.Drums![0..2]);
                        mixer.AddChannel(SongStem.Drums2, _rbMetadata.Indices.Drums[1..3], _rbMetadata.Panning.Drums[2..6]);
                        mixer.AddChannel(SongStem.Drums3, _rbMetadata.Indices.Drums[3..5], _rbMetadata.Panning.Drums[6..10]);
                        break;
                    //drum (0 1 2 3 4 5): stereo kick, stereo snare, stereo kit --> (0 1) (2 3) (4 5)
                    case 6:
                        mixer.AddChannel(SongStem.Drums1, _rbMetadata.Indices.Drums[0..2], _rbMetadata.Panning.Drums![0..4]);
                        mixer.AddChannel(SongStem.Drums2, _rbMetadata.Indices.Drums[2..4], _rbMetadata.Panning.Drums[4..8]);
                        mixer.AddChannel(SongStem.Drums3, _rbMetadata.Indices.Drums[4..6], _rbMetadata.Panning.Drums[8..12]);
                        break;
                }
            }

            if (_rbMetadata.Indices.Bass.Length > 0 && !ignoreStems.Contains(SongStem.Bass))
                mixer.AddChannel(SongStem.Bass, _rbMetadata.Indices.Bass, _rbMetadata.Panning.Bass!);

            if (_rbMetadata.Indices.Guitar.Length > 0 && !ignoreStems.Contains(SongStem.Guitar))
                mixer.AddChannel(SongStem.Guitar, _rbMetadata.Indices.Guitar, _rbMetadata.Panning.Guitar!);

            if (_rbMetadata.Indices.Keys.Length > 0 && !ignoreStems.Contains(SongStem.Keys))
                mixer.AddChannel(SongStem.Keys, _rbMetadata.Indices.Keys, _rbMetadata.Panning.Keys!);

            if (_rbMetadata.Indices.Vocals.Length > 0 && !ignoreStems.Contains(SongStem.Vocals))
                mixer.AddChannel(SongStem.Vocals, _rbMetadata.Indices.Vocals, _rbMetadata.Panning.Vocals!);

            if (_rbMetadata.Indices.Track.Length > 0 && !ignoreStems.Contains(SongStem.Song))
                mixer.AddChannel(SongStem.Song, _rbMetadata.Indices.Track, _rbMetadata.Panning.Track!);

            if (_rbMetadata.Indices.Crowd.Length > 0 && !ignoreStems.Contains(SongStem.Crowd))
                mixer.AddChannel(SongStem.Crowd, _rbMetadata.Indices.Crowd, _rbMetadata.Panning.Crowd!);

            if (mixer.Channels.Count == 0)
            {
                YargLogger.LogError("Failed to add any stems!");
                stream.Dispose();
                mixer.Dispose();
                return null;
            }
            YargLogger.LogFormatInfo("Loaded {0} stems", mixer.Channels.Count);
            return mixer;
        }

        public override StemMixer? LoadPreviewAudio(float speed)
        {
            return LoadAudio(speed, 0, SongStem.Crowd);
        }

        public virtual void Serialize(BinaryWriter writer, CategoryCacheWriteNode node)
        {
            writer.Write(_updateMidi != null);
            _updateMidi?.Serialize(writer);

            SerializeMetadata(writer, node);

            WriteUpdateInfo(_updateMogg, writer);
            WriteUpdateInfo(_updateMilo, writer);
            WriteUpdateInfo(_updateImage, writer);

            writer.Write(_rbMetadata.AnimTempo);
            writer.Write(_rbMetadata.SongID);
            writer.Write(_rbMetadata.VocalPercussionBank);
            writer.Write(_rbMetadata.VocalSongScrollSpeed);
            writer.Write(_rbMetadata.VocalGender);
            writer.Write(_rbMetadata.VocalTonicNote);
            writer.Write(_rbMetadata.SongTonality);
            writer.Write(_rbMetadata.TuningOffsetCents);
            writer.Write(_rbMetadata.VenueVersion);
            writer.Write(_rbMetadata.DrumBank);

            RBAudio<int>.WriteArray(in _rbMetadata.RealGuitarTuning, writer);
            RBAudio<int>.WriteArray(in _rbMetadata.RealBassTuning, writer);

            _rbMetadata.Indices.Serialize(writer);
            _rbMetadata.Panning.Serialize(writer);

            WriteStringArray(_rbMetadata.Soloes, writer);
            WriteStringArray(_rbMetadata.VideoVenues, writer);

            unsafe
            {
                fixed (RBCONDifficulties* ptr = &_rbDifficulties)
                {
                    var span = new ReadOnlySpan<byte>(ptr, sizeof(RBCONDifficulties));
                    writer.Write(span);
                }
            }
        }

        protected abstract Stream? GetMidiStream();
        protected abstract Stream? GetMoggStream();

        protected RBCONEntry(in ScanNode info, RBProUpgrade? upgrade, in HashWrapper hash)
            : base(in info, in hash)
        {
            _songLength = info.SongLength;
            _rbMetadata = info.RBMetadata;
            _rbDifficulties = info.Difficulties;
            _updateMidi = info.UpdateMidi;
            _updateMogg = info.UpdateMogg;
            _updateImage = info.UpdateImage;
            _updateMilo = info.UpdateMilo;
            _upgrade = upgrade;
        }

        protected RBCONEntry(AbridgedFileInfo? updateMidi, RBProUpgrade? upgrade, UnmanagedMemoryStream stream, CategoryCacheStrings strings)
            : base(stream, strings)
        {
            _updateMidi = updateMidi;
            _upgrade = upgrade;

            _updateMogg =  stream.ReadBoolean() ? new AbridgedFileInfo(stream.ReadString(), false) : null;
            _updateMilo =  stream.ReadBoolean() ? new AbridgedFileInfo(stream.ReadString(), false) : null;
            _updateImage = stream.ReadBoolean() ? new AbridgedFileInfo(stream.ReadString(), false) : null;

            _rbMetadata.AnimTempo = stream.Read<uint>(Endianness.Little);
            _rbMetadata.SongID = stream.ReadString();
            _rbMetadata.VocalPercussionBank = stream.ReadString();
            _rbMetadata.VocalSongScrollSpeed = stream.Read<uint>(Endianness.Little);
            _rbMetadata.VocalGender = stream.ReadBoolean();
            _rbMetadata.VocalTonicNote = stream.Read<uint>(Endianness.Little);
            _rbMetadata.SongTonality = stream.ReadBoolean();
            _rbMetadata.TuningOffsetCents = stream.Read<int>(Endianness.Little);
            _rbMetadata.VenueVersion = stream.Read<uint>(Endianness.Little);
            _rbMetadata.DrumBank = stream.ReadString();

            _rbMetadata.RealGuitarTuning = RBAudio<int>.ReadArray(stream);
            _rbMetadata.RealBassTuning = RBAudio<int>.ReadArray(stream);

            _rbMetadata.Indices = new RBAudio<int>(stream);
            _rbMetadata.Panning = new RBAudio<float>(stream);

            _rbMetadata.Soloes = ReadStringArray(stream);
            _rbMetadata.VideoVenues = ReadStringArray(stream);

            unsafe
            {
                fixed (RBCONDifficulties* ptr = &_rbDifficulties)
                {
                    var span = new Span<byte>(ptr, sizeof(RBCONDifficulties));
                    stream.Read(span);
                }
            }
        }

        public struct ScanNode
        {
            public static readonly ScanNode Default = new()
            {
                Metadata = SongMetadata.Default,
                RBMetadata = RBMetadata.Default,
                Settings = ParseSettings.Default,
                Parts = AvailableParts.Default,
                Difficulties = RBCONDifficulties.Default,
                YearAsNumber = int.MaxValue,
            };

            static ScanNode()
            {
                Default.Settings.DrumsType = DrumsType.FourLane;
                Default.Settings.NoteSnapThreshold = NOTE_SNAP_THRESHOLD;
            }

            public string? Location;
            public SongMetadata Metadata;
            public RBMetadata RBMetadata;
            public ParseSettings Settings;
            public AvailableParts Parts;
            public RBCONDifficulties Difficulties;

            public AbridgedFileInfo? UpdateMidi;
            public AbridgedFileInfo? UpdateMogg;
            public AbridgedFileInfo? UpdateMilo;
            public AbridgedFileInfo? UpdateImage;

            public int YearAsNumber;
            public ulong SongLength;
        }

        protected static (ScanResult Result, ScanNode Info) ProcessDTAs(string nodename, DTAEntry baseDTA, SongUpdate? update, DTAEntry? Upgrade)
        {
            float[]? volumes = null;
            float[]? pans = null;
            float[]? cores = null;

            var info = ScanNode.Default;
            void ParseDTA(DTAEntry entry)
            {
                if (entry.Name != null) { info.Metadata.Name = entry.Name; }
                if (entry.Artist != null) { info.Metadata.Artist = entry.Artist; }
                if (entry.Charter != null) { info.Metadata.Charter = entry.Charter; }
                if (entry.Genre != null) { info.Metadata.Genre = entry.Genre; }
                if (entry.YearAsNumber != null)
                {
                    info.YearAsNumber = entry.YearAsNumber.Value;
                    info.Metadata.Year = info.YearAsNumber.ToString();
                }
                if (entry.Source != null) { info.Metadata.Source = entry.Source; }
                if (entry.Playlist != null) { info.Metadata.Playlist = entry.Playlist; }
                if (entry.SongLength != null) { info.SongLength = entry.SongLength.Value; }
                if (entry.IsMaster != null) { info.Metadata.IsMaster = entry.IsMaster.Value; }
                if (entry.AlbumTrack != null) { info.Metadata.AlbumTrack = entry.AlbumTrack.Value; }
                if (entry.PreviewStart != null)
                {
                    info.Metadata.PreviewStart = entry.PreviewStart.Value;
                    info.Metadata.PreviewEnd = entry.PreviewEnd!.Value;
                }
                if (entry.HopoThreshold != null) { info.Settings.HopoThreshold = entry.HopoThreshold.Value; }
                if (entry.SongRating != null) { info.Metadata.SongRating = entry.SongRating.Value; }
                if (entry.VocalPercussionBank != null) { info.RBMetadata.VocalPercussionBank = entry.VocalPercussionBank; }
                if (entry.VocalGender != null) { info.RBMetadata.VocalGender = entry.VocalGender.Value; }
                if (entry.VocalSongScrollSpeed != null) { info.RBMetadata.VocalSongScrollSpeed = entry.VocalSongScrollSpeed.Value; }
                if (entry.VocalTonicNote != null) { info.RBMetadata.VocalTonicNote = entry.VocalTonicNote.Value; }
                if (entry.VideoVenues != null) { info.RBMetadata.VideoVenues = entry.VideoVenues; }
                if (entry.DrumBank != null) { info.RBMetadata.DrumBank = entry.DrumBank; }
                if (entry.SongID != null) { info.RBMetadata.SongID = entry.SongID; }
                if (entry.SongTonality != null) { info.RBMetadata.SongTonality = entry.SongTonality.Value; }
                if (entry.Soloes != null) { info.RBMetadata.Soloes = entry.Soloes; }
                if (entry.AnimTempo != null) { info.RBMetadata.AnimTempo = entry.AnimTempo.Value; }
                if (entry.TuningOffsetCents != null) { info.RBMetadata.TuningOffsetCents = entry.TuningOffsetCents.Value; }
                if (entry.RealGuitarTuning != null) { info.RBMetadata.RealGuitarTuning = entry.RealGuitarTuning; }
                if (entry.RealBassTuning != null) { info.RBMetadata.RealBassTuning = entry.RealBassTuning; }

                if (entry.Cores != null) { cores = entry.Cores; }
                if (entry.Volumes != null) { volumes = entry.Volumes; }
                if (entry.Pans != null) { pans = entry.Pans; }

                if (entry.Location != null) { info.Location = entry.Location; }

                if (entry.Indices != null)
                {
                    var crowd = info.RBMetadata.Indices.Crowd;
                    info.RBMetadata.Indices = entry.Indices.Value;
                    info.RBMetadata.Indices.Crowd = crowd;
                }

                if (entry.CrowdChannels != null) { info.RBMetadata.Indices.Crowd = entry.CrowdChannels; }

                if (entry.Difficulties.Band >= 0) info.Difficulties.Band = entry.Difficulties.Band;
                if (entry.Difficulties.FiveFretGuitar >= 0) info.Difficulties.FiveFretGuitar = entry.Difficulties.FiveFretGuitar;
                if (entry.Difficulties.FiveFretBass >= 0) info.Difficulties.FiveFretBass = entry.Difficulties.FiveFretBass;
                if (entry.Difficulties.FiveFretRhythm >= 0) info.Difficulties.FiveFretRhythm = entry.Difficulties.FiveFretRhythm;
                if (entry.Difficulties.FiveFretCoop >= 0) info.Difficulties.FiveFretCoop = entry.Difficulties.FiveFretCoop;
                if (entry.Difficulties.Keys >= 0) info.Difficulties.Keys = entry.Difficulties.Keys;
                if (entry.Difficulties.FourLaneDrums >= 0) info.Difficulties.FourLaneDrums = entry.Difficulties.FourLaneDrums;
                if (entry.Difficulties.ProDrums >= 0) info.Difficulties.ProDrums = entry.Difficulties.ProDrums;
                if (entry.Difficulties.ProGuitar >= 0) info.Difficulties.ProGuitar = entry.Difficulties.ProGuitar;
                if (entry.Difficulties.ProBass >= 0) info.Difficulties.ProBass = entry.Difficulties.ProBass;
                if (entry.Difficulties.ProKeys >= 0) info.Difficulties.ProKeys = entry.Difficulties.ProKeys;
                if (entry.Difficulties.LeadVocals >= 0) info.Difficulties.LeadVocals = entry.Difficulties.LeadVocals;
                if (entry.Difficulties.HarmonyVocals >= 0) info.Difficulties.HarmonyVocals = entry.Difficulties.HarmonyVocals;
            }

            ParseDTA(baseDTA);
            if (update != null)
            {
                ParseDTA(update.Entry);
                if (update.Entry.DiscUpdate)
                {
                    if (update.Midi != null)
                    {
                        if (info.UpdateMidi == null || update.Midi.Value.LastUpdatedTime > info.UpdateMidi.Value.LastUpdatedTime)
                        {
                            info.UpdateMidi = update.Midi;
                        }
                    }
                    else
                    {
                        YargLogger.LogFormatWarning("Update midi not found for {0}", nodename);
                    }
                }

                if (update.Mogg != null)
                {
                    if (info.UpdateMogg == null || update.Mogg.Value.LastUpdatedTime > info.UpdateMogg.Value.LastUpdatedTime)
                    {
                        info.UpdateMogg = update.Mogg;
                    }
                }

                if (update.Milo != null)
                {
                    if (info.UpdateMilo == null || update.Milo.Value.LastUpdatedTime > info.UpdateMilo.Value.LastUpdatedTime)
                    {
                        info.UpdateMilo = update.Milo;
                    }
                }

                if (update.Entry.AlternatePath)
                {
                    if (update.Image != null)
                    {
                        if (info.UpdateImage == null || update.Image.Value.LastUpdatedTime > info.UpdateImage.Value.LastUpdatedTime)
                        {
                            info.UpdateImage = update.Image;
                        }
                    }
                }
            }

            if (Upgrade != null)
            {
                ParseDTA(Upgrade);
            }

            if (info.Metadata.Name.Length == 0)
            {
                return (ScanResult.NoName, info);
            }

            if (info.Location == null || pans == null || volumes == null || cores == null)
            {
                return (ScanResult.DTAError, info);
            }

            if (info.Difficulties.FourLaneDrums > -1)
            {
                SetRank(ref info.Parts.FourLaneDrums.Intensity, info.Difficulties.FourLaneDrums, DrumDiffMap);
                if (info.Parts.ProDrums.Intensity == -1)
                {
                    info.Parts.ProDrums.Intensity = info.Parts.FourLaneDrums.Intensity;
                }
            }
            if (info.Difficulties.FiveFretGuitar > -1)
            {
                SetRank(ref info.Parts.FiveFretGuitar.Intensity, info.Difficulties.FiveFretGuitar, GuitarDiffMap);
                if (info.Parts.ProGuitar_17Fret.Intensity == -1)
                {
                    info.Parts.ProGuitar_22Fret.Intensity = info.Parts.ProGuitar_17Fret.Intensity = info.Parts.FiveFretGuitar.Intensity;
                }
            }
            if (info.Difficulties.FiveFretBass > -1)
            {
                SetRank(ref info.Parts.FiveFretBass.Intensity, info.Difficulties.FiveFretBass, GuitarDiffMap);
                if (info.Parts.ProBass_17Fret.Intensity == -1)
                {
                    info.Parts.ProBass_22Fret.Intensity = info.Parts.ProBass_17Fret.Intensity = info.Parts.FiveFretGuitar.Intensity;
                }
            }
            if (info.Difficulties.LeadVocals > -1)
            {
                SetRank(ref info.Parts.LeadVocals.Intensity, info.Difficulties.LeadVocals, GuitarDiffMap);
                if (info.Parts.HarmonyVocals.Intensity == -1)
                {
                    info.Parts.HarmonyVocals.Intensity = info.Parts.LeadVocals.Intensity;
                }
            }
            if (info.Difficulties.Keys > -1)
            {
                SetRank(ref info.Parts.Keys.Intensity, info.Difficulties.Keys, GuitarDiffMap);
                if (info.Parts.ProKeys.Intensity == -1)
                {
                    info.Parts.ProKeys.Intensity = info.Parts.Keys.Intensity;
                }
            }
            if (info.Difficulties.ProGuitar > -1)
            {
                SetRank(ref info.Parts.ProGuitar_17Fret.Intensity, info.Difficulties.ProGuitar, RealGuitarDiffMap);
                info.Parts.ProGuitar_22Fret.Intensity = info.Parts.ProGuitar_17Fret.Intensity;
                if (info.Parts.FiveFretGuitar.Intensity == -1)
                {
                    info.Parts.FiveFretGuitar.Intensity = info.Parts.ProGuitar_17Fret.Intensity;
                }
            }
            if (info.Difficulties.ProBass > -1)
            {
                SetRank(ref info.Parts.ProBass_17Fret.Intensity, info.Difficulties.ProBass, RealGuitarDiffMap);
                info.Parts.ProBass_22Fret.Intensity = info.Parts.ProBass_17Fret.Intensity;
                if (info.Parts.FiveFretBass.Intensity == -1)
                {
                    info.Parts.FiveFretBass.Intensity = info.Parts.ProBass_17Fret.Intensity;
                }
            }
            if (info.Difficulties.ProKeys > -1)
            {
                SetRank(ref info.Parts.ProKeys.Intensity, info.Difficulties.ProKeys, RealKeysDiffMap);
                if (info.Parts.Keys.Intensity == -1)
                {
                    info.Parts.Keys.Intensity = info.Parts.ProKeys.Intensity;
                }
            }
            if (info.Difficulties.ProDrums > -1)
            {
                SetRank(ref info.Parts.ProDrums.Intensity, info.Difficulties.ProDrums, DrumDiffMap);
                if (info.Parts.FourLaneDrums.Intensity == -1)
                {
                    info.Parts.FourLaneDrums.Intensity = info.Parts.ProDrums.Intensity;
                }
            }
            if (info.Difficulties.HarmonyVocals > -1)
            {
                SetRank(ref info.Parts.HarmonyVocals.Intensity, info.Difficulties.HarmonyVocals, DrumDiffMap);
                if (info.Parts.LeadVocals.Intensity == -1)
                {
                    info.Parts.LeadVocals.Intensity = info.Parts.HarmonyVocals.Intensity;
                }
            }
            if (info.Difficulties.Band > -1)
            {
                SetRank(ref info.Parts.BandDifficulty.Intensity, info.Difficulties.Band, BandDiffMap);
                info.Parts.BandDifficulty.SubTracks = 1;
            }

            unsafe
            {
                var usedIndices = stackalloc bool[pans.Length];
                float[] CalculateStemValues(int[] indices)
                {
                    float[] values = new float[2 * indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                    {
                        float theta = (pans[indices[i]] + 1) * ((float) Math.PI / 4);
                        float volRatio = (float) Math.Pow(10, volumes[indices[i]] / 20);
                        values[2 * i] = volRatio * (float) Math.Cos(theta);
                        values[2 * i + 1] = volRatio * (float) Math.Sin(theta);
                        usedIndices[indices[i]] = true;
                    }
                    return values;
                }

                if (info.RBMetadata.Indices.Drums.Length > 0)
                {
                    info.RBMetadata.Panning.Drums = CalculateStemValues(info.RBMetadata.Indices.Drums);
                }

                if (info.RBMetadata.Indices.Bass.Length > 0)
                {
                    info.RBMetadata.Panning.Bass = CalculateStemValues(info.RBMetadata.Indices.Bass);
                }

                if (info.RBMetadata.Indices.Guitar.Length > 0)
                {
                    info.RBMetadata.Panning.Guitar = CalculateStemValues(info.RBMetadata.Indices.Guitar);
                }

                if (info.RBMetadata.Indices.Keys.Length > 0)
                {
                    info.RBMetadata.Panning.Keys = CalculateStemValues(info.RBMetadata.Indices.Keys);
                }

                if (info.RBMetadata.Indices.Vocals.Length > 0)
                {
                    info.RBMetadata.Panning.Vocals = CalculateStemValues(info.RBMetadata.Indices.Vocals);
                }

                if (info.RBMetadata.Indices.Crowd.Length > 0)
                {
                    info.RBMetadata.Panning.Crowd = CalculateStemValues(info.RBMetadata.Indices.Crowd);
                }

                var leftover = new List<int>(pans.Length);
                for (int i = 0; i < pans.Length; i++)
                {
                    if (!usedIndices[i])
                    {
                        leftover.Add(i);
                    }
                }

                if (leftover.Count > 0)
                {
                    info.RBMetadata.Indices.Track = leftover.ToArray();
                    info.RBMetadata.Panning.Track = CalculateStemValues(info.RBMetadata.Indices.Track);
                }
            }
            
            return (ScanResult.Success, info);
        }

        protected static Stream? LoadUpdateMoggStream(in AbridgedFileInfo? info)
        {
            if (info == null)
            {
                return null;
            }

            var mogg = info.Value;
            if (!File.Exists(mogg.FullName))
            {
                return null;
            }

            if (mogg.FullName.EndsWith(".yarg_mogg"))
            {
                return new YargMoggReadStream(mogg.FullName);
            }
            return new FileStream(mogg.FullName, FileMode.Open, FileAccess.Read);
        }

        protected static (ScanResult Result, HashWrapper Hash) ParseRBCONMidi(in FixedArray<byte> mainMidi, in AbridgedFileInfo? update, RBProUpgrade? upgrade, ref AvailableParts parts)
        {
            try
            {
                if (update.HasValue && !update.Value.IsStillValid(false))
                {
                    return (ScanResult.MissingUpdateMidi, default);
                }

                using var upgradeMidi = upgrade != null ? upgrade.LoadUpgradeMidi() : FixedArray<byte>.Null;
                if (upgrade != null && !upgradeMidi.IsAllocated)
                {
                    return (ScanResult.MissingUpgradeMidi, default);
                }

                DrumPreparseHandler drumTracker = new()
                {
                    Type = DrumsType.ProDrums
                };

                long bufLength = 0;
                using var updateMidi = update.HasValue ? FixedArray<byte>.Load(update.Value.FullName) : FixedArray<byte>.Null;
                if (updateMidi.IsAllocated)
                {
                    if (!ParseMidi(in updateMidi, drumTracker, ref parts))
                    {
                        return (ScanResult.MultipleMidiTrackNames_Update, default);
                    }
                    bufLength += updateMidi.Length;
                }

                if (upgradeMidi.IsAllocated)
                {
                    if (!ParseMidi(in upgradeMidi, drumTracker, ref parts))
                    {
                        return (ScanResult.MultipleMidiTrackNames_Upgrade, default);
                    }
                    bufLength += upgradeMidi.Length;
                }

                if (!ParseMidi(in mainMidi, drumTracker, ref parts))
                {
                    return (ScanResult.MultipleMidiTrackNames, default);
                }
                bufLength += mainMidi.Length;

                SetDrums(ref parts, drumTracker);
                if (!CheckScanValidity(in parts))
                {
                    return (ScanResult.NoNotes, default);
                }

                using var buffer = FixedArray<byte>.Alloc(bufLength);
                unsafe
                {
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(buffer.Ptr, mainMidi.Ptr, (uint) mainMidi.Length);

                    long offset = mainMidi.Length;
                    if (updateMidi.IsAllocated)
                    {
                        System.Runtime.CompilerServices.Unsafe.CopyBlock(buffer.Ptr + offset, updateMidi.Ptr, (uint) updateMidi.Length);
                        offset += updateMidi.Length;
                    }

                    if (upgradeMidi.IsAllocated)
                    {
                        System.Runtime.CompilerServices.Unsafe.CopyBlock(buffer.Ptr + offset, upgradeMidi.Ptr, (uint) upgradeMidi.Length);
                    }
                }
                return (ScanResult.Success, HashWrapper.Hash(buffer.ReadOnlySpan));
            }
            catch
            {
                return (ScanResult.PossibleCorruption, default);
            }
        }

        private static readonly int[] BandDiffMap = { 163, 215, 243, 267, 292, 345 };
        private static readonly int[] GuitarDiffMap = { 139, 176, 221, 267, 333, 409 };
        private static readonly int[] BassDiffMap = { 135, 181, 228, 293, 364, 436 };
        private static readonly int[] DrumDiffMap = { 124, 151, 178, 242, 345, 448 };
        private static readonly int[] KeysDiffMap = { 153, 211, 269, 327, 385, 443 };
        private static readonly int[] VocalsDiffMap = { 132, 175, 218, 279, 353, 427 };
        private static readonly int[] RealGuitarDiffMap = { 150, 205, 264, 323, 382, 442 };
        private static readonly int[] RealBassDiffMap = { 150, 208, 267, 325, 384, 442 };
        private static readonly int[] RealDrumsDiffMap = { 124, 151, 178, 242, 345, 448 };
        private static readonly int[] RealKeysDiffMap = { 153, 211, 269, 327, 385, 443 };
        private static readonly int[] HarmonyDiffMap = { 132, 175, 218, 279, 353, 427 };

        private static void SetRank(ref sbyte intensity, int rank, int[] values)
        {
            sbyte i = 0;
            while (i < 6 && values[i] <= rank)
                ++i;
            intensity = i;
        }

        private static AbridgedFileInfo? ReadUpdateInfo(UnmanagedMemoryStream stream)
        {
            if (!stream.ReadBoolean())
            {
                return null;
            }
            return new AbridgedFileInfo(stream.ReadString(), false);
        }

        private static string[] ReadStringArray(UnmanagedMemoryStream stream)
        {
            int length = stream.Read<int>(Endianness.Little);
            if (length == 0)
            {
                return Array.Empty<string>();
            }

            var strings = new string[length];
            for (int i = 0; i < length; ++i)
                strings[i] = stream.ReadString();
            return strings;
        }

        private static void WriteUpdateInfo<TInfo>(TInfo? info, BinaryWriter writer)
            where TInfo : struct, IAbridgedInfo
        {
            if (info != null)
            {
                writer.Write(true);
                writer.Write(info.Value.FullName);
            }
            else
                writer.Write(false);
        }

        private static void WriteStringArray(string[] strings, BinaryWriter writer)
        {
            writer.Write(strings.Length);
            for (int i = 0; i < strings.Length; ++i)
            {
                writer.Write(strings[i]);
            }
        }
    }
}
