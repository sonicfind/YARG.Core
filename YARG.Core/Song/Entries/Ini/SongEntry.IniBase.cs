﻿using MoonscraperChartEditor.Song.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YARG.Core.Chart;
using YARG.Core.Extensions;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Song.Cache;
using YARG.Core.Song.Preparsers;

namespace YARG.Core.Song
{
    public static class IniAudio
    {
        public static readonly string[] SupportedStems = { "song", "guitar", "bass", "rhythm", "keys", "vocals", "vocals_1", "vocals_2", "drums", "drums_1", "drums_2", "drums_3", "drums_4", "crowd", };
        public static readonly string[] SupportedFormats = { ".opus", ".ogg", ".mp3", ".wav", ".aiff", };
        private static readonly HashSet<string> SupportedAudioFiles = new();

        static IniAudio()
        {
            foreach (string stem in SupportedStems)
                foreach (string format in SupportedFormats)
                    SupportedAudioFiles.Add(stem + format);
        }

        public static bool IsAudioFile(string file)
        {
            return SupportedAudioFiles.Contains(file);
        }
    }

    public abstract class IniSubEntry : SongEntry
    {
        public static readonly (string Filename, ChartFormat Format)[] CHART_FILE_TYPES =
        {
            ("notes.mid"  , ChartFormat.Mid),
            ("notes.midi" , ChartFormat.Midi),
            ("notes.chart", ChartFormat.Chart),
        };

        protected static readonly string[] ALBUMART_FILES;
        protected static readonly string[] PREVIEW_FILES;

        static IniSubEntry()
        {
            ALBUMART_FILES = new string[IMAGE_EXTENSIONS.Length];
            for (int i = 0; i < ALBUMART_FILES.Length; i++)
            {
                ALBUMART_FILES[i] = "album" + IMAGE_EXTENSIONS[i];
            }

            PREVIEW_FILES = new string[IniAudio.SupportedFormats.Length];
            for (int i = 0; i < PREVIEW_FILES.Length; i++)
            {
                PREVIEW_FILES[i] = "preview" + IniAudio.SupportedFormats[i];
            }
        }

        protected readonly ChartFormat _chartFormat;
        protected string _background = string.Empty;
        protected string _video = string.Empty;
        protected string _cover = string.Empty;

        protected abstract FixedArray<byte> GetChartData(string filename);

        protected IniSubEntry(ChartFormat chartFormat)
        {
            _chartFormat = chartFormat;
        }

        protected new void Deserialize(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            base.Deserialize(stream, strings);
            _background = stream.ReadString();
            _video = stream.ReadString();
            _cover = stream.ReadString();
            (_parsedYear, _yearAsNumber) = ParseYear(_metadata.Year);
        }

        public override void Serialize(MemoryStream stream, CacheWriteIndices indices)
        {
            base.Serialize(stream, indices);
            stream.Write(_background);
            stream.Write(_video);
            stream.Write(_cover);
        }

        public override SongChart? LoadChart()
        {
            using var data = GetChartData(CHART_FILE_TYPES[(int) _chartFormat].Filename);
            if (!data.IsAllocated)
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

            if (_chartFormat == ChartFormat.Mid || _chartFormat == ChartFormat.Midi)
            {
                return SongChart.FromMidi(in parseSettings, MidFileLoader.LoadMidiFile(stream));
            }

            using var reader = new StreamReader(stream);
            return SongChart.FromDotChart(in parseSettings, reader.ReadToEnd());
        }

        public override FixedArray<byte> LoadMiloData()
        {
            return FixedArray<byte>.Null;
        }

        protected static unsafe ScanResult ScanChart(IniSubEntry entry, FixedArray<byte>* file, IniModifierCollection modifiers)
        {
            var drums_type = DrumsType.Unknown;
            if (modifiers.Extract("five_lane_drums", out bool fiveLaneDrums))
            {
                drums_type = fiveLaneDrums ? DrumsType.FiveLane : DrumsType.FourLane;
            }

            var results = default((ScanResult result, long resolution));
            if (entry._chartFormat == ChartFormat.Chart)
            {
                if (YARGTextReader.TryUTF8(file, out var byteContainer))
                {
                    results = ParseDotChart(ref byteContainer, modifiers, ref entry._parts, ref drums_type);
                }
                else
                {
                    using var chars = YARGTextReader.TryUTF16Cast(in *file);
                    if (chars.IsAllocated)
                    {
                        var charContainer = YARGTextReader.CreateUTF16Container(&chars);
                        results = ParseDotChart(ref charContainer, modifiers, ref entry._parts, ref drums_type);
                    }
                    else
                    {
                        using var ints = YARGTextReader.CastUTF32(in *file);
                        var intContainer = YARGTextReader.CreateUTF32Container(&ints);
                        results = ParseDotChart(ref intContainer, modifiers, ref entry._parts, ref drums_type);
                    }
                }
            }
            else // if (chartType == ChartType.Mid || chartType == ChartType.Midi) // Uncomment for any future file type
            {
                results = ParseDotMidi(in *file, modifiers, ref entry._parts, ref drums_type);
            }

            if (results.result != ScanResult.Success)
            {
                return results.result;
            }

            FinalizeDrums(ref entry._parts, drums_type);
            if (!IsValid(in entry._parts))
            {
                return ScanResult.NoNotes;
            }

            if (!modifiers.Contains("name"))
            {
                return ScanResult.NoName;
            }

            SongMetadata.FillFromIni(ref entry._metadata, modifiers);
            SetIntensities(modifiers, ref entry._parts);

            (entry._parsedYear, entry._yearAsNumber) = ParseYear(entry._metadata.Year);
            entry._hash = HashWrapper.Hash(file->ReadOnlySpan);
            entry.SetSearchStrings();

            if (!modifiers.Extract("hopo_frequency", out entry._settings.HopoThreshold) || entry._settings.HopoThreshold <= 0)
            {
                if (modifiers.Extract("eighthnote_hopo", out bool eighthNoteHopo))
                {
                    entry._settings.HopoThreshold = results.resolution / (eighthNoteHopo ? 2 : 3);
                }
                else if (modifiers.Extract("hopofreq", out long hopoFreq))
                {
                    int denominator = hopoFreq switch
                    {
                        0 => 24,
                        1 => 16,
                        2 => 12,
                        3 => 8,
                        4 => 6,
                        5 => 4,
                        _ => throw new NotImplementedException($"Unhandled hopofreq value {hopoFreq}!")
                    };
                    entry._settings.HopoThreshold = 4 * results.resolution / denominator;
                }
                else
                {
                    entry._settings.HopoThreshold = results.resolution / 3;
                }

                if (entry._chartFormat == ChartFormat.Chart)
                {
                    // With a 192 resolution, .chart has a HOPO threshold of 65 ticks, not 64,
                    // so we need to scale this factor to different resolutions (480 res = 162.5 threshold).
                    // Why?... idk, but I hate it.
                    const float DEFAULT_RESOLUTION = 192;
                    entry._settings.HopoThreshold += (long) (results.resolution / DEFAULT_RESOLUTION);
                }
            }

            // .chart defaults to no sustain cutoff whatsoever if the ini does not define the value.
            // Since a failed `Extract` sets the value to zero, we would need no additional work unless it's .mid
            if (!modifiers.Extract("sustain_cutoff_threshold", out entry._settings.SustainCutoffThreshold) && entry._chartFormat != ChartFormat.Chart)
            {
                entry._settings.SustainCutoffThreshold = results.resolution / 3;
            }

            if (entry._chartFormat == ChartFormat.Mid || entry._chartFormat == ChartFormat.Midi)
            {
                if (!modifiers.Extract("multiplier_note", out entry._settings.OverdiveMidiNote) || entry._settings.OverdiveMidiNote != 103)
                {
                    entry._settings.OverdiveMidiNote = 116;
                }
            }

            if (modifiers.Extract("background", out string background))
            {
                entry._background = background;
            }

            if (modifiers.Extract("video", out string video))
            {
                entry._video = video;
            }

            if (modifiers.Extract("cover", out string cover))
            {
                entry._cover = cover;
            }

            if (entry._metadata.SongLength <= 0)
            {
                using var mixer = entry.LoadAudio(0, 0);
                if (mixer != null)
                {
                    entry._metadata.SongLength = (long) (mixer.Length * SongMetadata.MILLISECOND_FACTOR);
                }
            }
            return ScanResult.Success;
        }

        protected static bool TryGetRandomBackgroundImage<TEnumerable, TValue>(in TEnumerable collection, out TValue? value)
            where TEnumerable : IEnumerable<KeyValuePair<string, TValue>>
        {
            // Choose a valid image background present in the folder at random
            var images = new List<TValue>();
            foreach (var format in IMAGE_EXTENSIONS)
            {
                var (_, image) = collection.FirstOrDefault(node => node.Key == "bg" + format);
                if (image != null)
                {
                    images.Add(image);
                }
            }

            foreach (var (shortname, image) in collection)
            {
                if (!shortname.StartsWith("background"))
                {
                    continue;
                }

                foreach (var format in IMAGE_EXTENSIONS)
                {
                    if (shortname.EndsWith(format))
                    {
                        images.Add(image);
                        break;
                    }
                }
            }

            if (images.Count == 0)
            {
                value = default!;
                return false;
            }
            value = images[SongEntry.BACKROUND_RNG.Next(images.Count)];
            return true;
        }

        protected static DrumsType ParseDrumsType(in AvailableParts parts)
        {
            if (parts.FourLaneDrums.IsActive())
            {
                return DrumsType.FourLane;
            }
            if (parts.FiveLaneDrums.IsActive())
            {
                return DrumsType.FiveLane;
            }
            return DrumsType.Unknown;
        }

        private static (ScanResult result, long resolution) ParseDotChart<TChar>(ref YARGTextContainer<TChar> container, IniModifierCollection modifiers, ref AvailableParts parts, ref DrumsType drumsType)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            long resolution = 192;
            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.HEADERTRACK))
            {
                var chartMods = YARGChartFileReader.ExtractModifiers(ref container);
                if (chartMods.Extract("Resolution", out long res))
                {
                    resolution = res;
                    if (resolution < 1)
                    {
                        return (ScanResult.InvalidResolution, 0);
                    }
                }
                modifiers.Union(chartMods);
            }

            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (!TraverseChartTrack(ref container, ref parts, ref drumsType))
                {
                    YARGChartFileReader.SkipToNextTrack(ref container);
                }
            }

            if (drumsType == DrumsType.Unknown && parts.FourLaneDrums.Difficulties > DifficultyMask.None)
            {
                drumsType = DrumsType.FourLane;
            }
            return (ScanResult.Success, resolution);
        }
        private static (ScanResult result, long resolution) ParseDotMidi(in FixedArray<byte> file, IniModifierCollection modifiers, ref AvailableParts parts, ref DrumsType drumsType)
        {
            if (!modifiers.Extract("pro_drums", out bool proDrums) || proDrums)
            {
                if (drumsType == DrumsType.Unknown)
                {
                    drumsType = DrumsType.UnknownPro;
                }
                else if (drumsType == DrumsType.FourLane)
                {
                    drumsType = DrumsType.ProDrums;
                }
            }
            return ParseMidi(in file, ref parts, ref drumsType);
        }

        /// <returns>Whether the track was fully traversed</returns>
        private static unsafe bool TraverseChartTrack<TChar>(ref YARGTextContainer<TChar> container, ref AvailableParts parts, ref DrumsType drumsType)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!YARGChartFileReader.ValidateInstrument(ref container, out var instrument, out var difficulty))
            {
                return false;
            }

            return instrument switch
            {
                Instrument.FiveFretGuitar     => ScanFiveFret(ref parts.FiveFretGuitar,               ref container, difficulty),
                Instrument.FiveFretBass       => ScanFiveFret(ref parts.FiveFretBass,                 ref container, difficulty),
                Instrument.FiveFretRhythm     => ScanFiveFret(ref parts.FiveFretRhythm,               ref container, difficulty),
                Instrument.FiveFretCoopGuitar => ScanFiveFret(ref parts.FiveFretCoopGuitar,           ref container, difficulty),
                Instrument.Keys               => ScanFiveFret(ref parts.Keys,                         ref container, difficulty),
                Instrument.SixFretGuitar      => ScanSixFret (ref parts.SixFretGuitar,                ref container, difficulty),
                Instrument.SixFretBass        => ScanSixFret (ref parts.SixFretBass,                  ref container, difficulty),
                Instrument.SixFretRhythm      => ScanSixFret (ref parts.SixFretRhythm,                ref container, difficulty),
                Instrument.SixFretCoopGuitar  => ScanSixFret (ref parts.SixFretCoopGuitar,            ref container, difficulty),
                Instrument.FourLaneDrums      => ScanDrums   (ref parts.FourLaneDrums, ref drumsType, ref container, difficulty),
                _ => false,
            };
        }

        private const int GUITAR_FIVEFRET_MAX = 5;
        private const int OPEN_NOTE = 7;
        private static bool ScanFiveFret<TChar>(ref PartValues part, ref YARGTextContainer<TChar> container, Difficulty difficulty)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (part[difficulty])
            {
                return false;
            }

            var ev = default(DotChartEvent);
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Note)
                {
                    uint lane = YARGChartFileReader.ExtractWithWhitespace<TChar, uint>(ref container);
                    ulong _ = YARGChartFileReader.Extract<TChar, ulong>(ref container);
                    if (lane < GUITAR_FIVEFRET_MAX || lane == OPEN_NOTE)
                    {
                        part.ActivateDifficulty(difficulty);
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool ScanSixFret<TChar>(ref PartValues part, ref YARGTextContainer<TChar> container, Difficulty difficulty)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            const int SIX_FRET_BLACK1 = 8;
            if (part[difficulty])
            {
                return false;
            }

            var ev = default(DotChartEvent);
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Note)
                {
                    uint lane = YARGChartFileReader.ExtractWithWhitespace<TChar, uint>(ref container);
                    ulong _ = YARGChartFileReader.Extract<TChar, ulong>(ref container);
                    if (lane < GUITAR_FIVEFRET_MAX || lane == SIX_FRET_BLACK1 || lane == OPEN_NOTE)
                    {
                        part.ActivateDifficulty(difficulty);
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool ScanDrums<TChar>(ref PartValues part, ref DrumsType drumsType, ref YARGTextContainer<TChar> container, Difficulty difficulty)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            const int YELLOW_CYMBAL = 66;
            const int GREEN_CYMBAL = 68;
            const int DOUBLE_BASS_MODIFIER = 32;

            var diff_mask = (DifficultyMask)(1 << (int)difficulty);
            // No point in scan a difficulty that already exists
            if ((part.Difficulties & diff_mask) > DifficultyMask.None)
            {
                return false;
            }

            var requiredMask = diff_mask;
            if (difficulty == Difficulty.Expert)
            {
                requiredMask |= DifficultyMask.ExpertPlus;
            }

            var ev = default(DotChartEvent);
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Note)
                {
                    uint lane = YARGChartFileReader.ExtractWithWhitespace<TChar, uint>(ref container);
                    ulong _ = YARGChartFileReader.Extract<TChar, ulong>(ref container);
                    if (0 <= lane && lane <= 4)
                    {
                        part.Difficulties |= diff_mask;
                    }
                    else if (lane == 5)
                    {
                        if (drumsType == DrumsType.FiveLane || drumsType == DrumsType.Unknown)
                        {
                            drumsType = DrumsType.FiveLane;
                            part.Difficulties |= diff_mask;
                        }
                    }
                    else if (YELLOW_CYMBAL <= lane && lane <= GREEN_CYMBAL)
                    {
                        if (drumsType != DrumsType.FiveLane)
                        {
                            drumsType = DrumsType.ProDrums;
                        }
                    }
                    else if (lane == DOUBLE_BASS_MODIFIER)
                    {
                        if (difficulty == Difficulty.Expert)
                        {
                            part.Difficulties |= DifficultyMask.ExpertPlus;
                        }
                    }

                    //  Testing against zero would not work in expert
                    if ((part.Difficulties & requiredMask) == requiredMask && (drumsType == DrumsType.ProDrums || drumsType == DrumsType.FiveLane))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void SetIntensities(IniModifierCollection modifiers, ref AvailableParts parts)
        {
            if (modifiers.Extract("diff_band", out int intensity))
            {
                parts.BandDifficulty.Intensity = (sbyte) intensity;
                if (intensity != -1)
                {
                    parts.BandDifficulty.SubTracks = 1;
                }
            }

            if (modifiers.Extract("diff_guitar", out intensity))
            {
                parts.ProGuitar_22Fret.Intensity = parts.ProGuitar_17Fret.Intensity = parts.FiveFretGuitar.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_bass", out intensity))
            {
                parts.ProBass_22Fret.Intensity = parts.ProBass_17Fret.Intensity = parts.FiveFretBass.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_rhythm", out intensity))
            {
                parts.FiveFretRhythm.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_guitar_coop", out intensity))
            {
                parts.FiveFretCoopGuitar.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_guitarghl", out intensity))
            {
                parts.SixFretGuitar.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_bassghl", out intensity))
            {
                parts.SixFretBass.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_rhythm_ghl", out intensity))
            {
                parts.SixFretRhythm.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_guitar_coop_ghl", out intensity))
            {
                parts.SixFretCoopGuitar.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_keys", out intensity))
            {
                parts.ProKeys.Intensity = parts.Keys.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_drums", out intensity))
            {
                parts.FourLaneDrums.Intensity = (sbyte) intensity;
                parts.ProDrums.Intensity = (sbyte) intensity;
                parts.FiveLaneDrums.Intensity = (sbyte) intensity;
                parts.EliteDrums.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_drums_real", out intensity) && intensity != -1)
            {
                parts.ProDrums.Intensity = (sbyte) intensity;
                parts.EliteDrums.Intensity = (sbyte) intensity;
                if (parts.FourLaneDrums.Intensity == -1)
                {
                    parts.FourLaneDrums.Intensity = parts.ProDrums.Intensity;
                }
            }

            if (modifiers.Extract("diff_guitar_real", out intensity) && intensity != -1)
            {
                parts.ProGuitar_22Fret.Intensity = parts.ProGuitar_17Fret.Intensity = (sbyte) intensity;
                if (parts.FiveFretGuitar.Intensity == -1)
                {
                    parts.FiveFretGuitar.Intensity = parts.ProGuitar_17Fret.Intensity;
                }
            }

            if (modifiers.Extract("diff_bass_real", out intensity) && intensity != -1)
            {
                parts.ProBass_22Fret.Intensity = parts.ProBass_17Fret.Intensity = (sbyte) intensity;
                if (parts.FiveFretBass.Intensity == -1)
                {
                    parts.FiveFretBass.Intensity = parts.ProBass_17Fret.Intensity;
                }
            }

            if (modifiers.Extract("diff_guitar_real_22", out intensity) && intensity != -1)
            {
                parts.ProGuitar_22Fret.Intensity = (sbyte) intensity;
                if (parts.ProGuitar_17Fret.Intensity == -1)
                {
                    parts.ProGuitar_17Fret.Intensity = parts.ProGuitar_22Fret.Intensity;
                }

                if (parts.FiveFretGuitar.Intensity == -1)
                {
                    parts.FiveFretGuitar.Intensity = parts.ProGuitar_22Fret.Intensity;
                }
            }

            if (modifiers.Extract("diff_bass_real_22", out intensity) && intensity != -1)
            {
                parts.ProBass_22Fret.Intensity = (sbyte) intensity;
                if (parts.ProBass_17Fret.Intensity == -1)
                {
                    parts.ProBass_17Fret.Intensity = parts.ProBass_22Fret.Intensity;
                }

                if (parts.FiveFretBass.Intensity == -1)
                {
                    parts.FiveFretBass.Intensity = parts.ProBass_22Fret.Intensity;
                }
            }

            if (modifiers.Extract("diff_keys_real", out intensity) && intensity != -1)
            {
                parts.ProKeys.Intensity = (sbyte) intensity;
                if (parts.Keys.Intensity == -1)
                {
                    parts.Keys.Intensity = parts.ProKeys.Intensity;
                }
            }

            if (modifiers.Extract("diff_vocals", out intensity))
            {
                parts.HarmonyVocals.Intensity = parts.LeadVocals.Intensity = (sbyte) intensity;
            }

            if (modifiers.Extract("diff_vocals_harm", out intensity) && intensity != -1)
            {
                parts.HarmonyVocals.Intensity = (sbyte) intensity;
                if (parts.LeadVocals.Intensity == -1)
                {
                    parts.LeadVocals.Intensity = parts.HarmonyVocals.Intensity;
                }
            }
        }

        private static (string Parsed, int AsNumber) ParseYear(string str)
        {
            for (int start = 0; start <= str.Length - MINIMUM_YEAR_DIGITS; ++start)
            {
                int curr = start;
                int number = 0;
                while (curr < str.Length && char.IsDigit(str[curr]))
                {
                    unchecked
                    {
                        number = 10 * number + str[curr] - '0';
                    }
                    ++curr;
                }

                if (curr >= start + MINIMUM_YEAR_DIGITS)
                {
                    return (str[start..curr], number);
                }
            }
            return (str, int.MaxValue);
        }
    }
}
