using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Parsing.Drums;

namespace YARG.Core.Parsing
{
    public static partial class DotChartLoader
    {
        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";

        //private const string DEFAULT_NAME = "Unknown Title";
        //private const string DEFAULT_ARTIST = "Unknown Artist";
        //private const string DEFAULT_ALBUM = "Unknown Album";
        //private const string DEFAULT_GENRE = "Unknown Genre";
        //private const string DEFAULT_YEAR = "Unknown Year";
        //private const string DEFAULT_CHARTER = "Unknown Charter";

        private const string SECTION = "section ";
        private const string LYRIC = "lyric ";
        private const string PHRASE_START = "phrase_start";
        private const string PHRASE_END = "phrase_end ";

        public static YARGChart Load(string filename, ParseSettings? parseSettings, Dictionary<NoteTracks_Chart, HashSet<Difficulty>>? activeTracks, bool doVocals)
        {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
            return Load(stream, parseSettings, activeTracks, doVocals);
        }

        public static YARGChart Load(Stream stream, ParseSettings? parseSettings, Dictionary<NoteTracks_Chart, HashSet<Difficulty>>? activeTracks, bool doVocals)
        {
            using var bytes = DisposableArray<byte>.Create(stream);
            using var byteReader = YARGTextLoader.TryLoadByteText(bytes);
            if (byteReader != null)
                return Load<byte, DotChartByte, ByteStringDecoder>(byteReader, parseSettings, activeTracks, doVocals);

            using var charReader = YARGTextLoader.LoadCharText(bytes);
            return Load<char, DotChartChar, CharStringDecoder>(charReader, parseSettings, activeTracks, doVocals);
        }

        private static readonly Dictionary<string, IniModifierCreator> MODIFIER_LIST = new()
        {
            { "Album",         new("album", ModifierCreatorType.String_Chart ) },
            { "Artist",        new("artist", ModifierCreatorType.String_Chart ) },
            { "BassStream",    new("BassStream", ModifierCreatorType.String_Chart ) },
            { "Charter",       new("charter", ModifierCreatorType.String_Chart ) },
            { "CrowdStream",   new("CrowdStream", ModifierCreatorType.String_Chart ) },
            { "Difficulty",    new("diff_band", ModifierCreatorType.Int32 ) },
            { "Drum2Stream",   new("Drum2Stream", ModifierCreatorType.String_Chart ) },
            { "Drum3Stream",   new("Drum3Stream", ModifierCreatorType.String_Chart ) },
            { "Drum4Stream",   new("Drum4Stream", ModifierCreatorType.String_Chart ) },
            { "DrumStream",    new("DrumStream", ModifierCreatorType.String_Chart ) },
            { "Genre",         new("genre", ModifierCreatorType.String_Chart ) },
            { "GuitarStream",  new("GuitarStream", ModifierCreatorType.String_Chart ) },
            { "HarmonyStream", new("HarmonyStream", ModifierCreatorType.String_Chart ) },
            { "KeysStream",    new("KeysStream", ModifierCreatorType.String_Chart ) },
            { "MusicStream",   new("MusicStream", ModifierCreatorType.String_Chart ) },
            { "Name",          new("name", ModifierCreatorType.String_Chart ) },
            { "Offset",        new("offset", ModifierCreatorType.Float ) },
            { "PreviewEnd",    new("previewEnd", ModifierCreatorType.Float ) },
            { "PreviewStart",  new("previewStart", ModifierCreatorType.Float ) },
            { "Resolution",    new("Resolution", ModifierCreatorType.UInt16 ) },
            { "RhythmStream",  new("RhythmStream", ModifierCreatorType.String_Chart ) },
            { "VocalStream",   new("VocalStream", ModifierCreatorType.String_Chart ) },
            { "Year",          new("year", ModifierCreatorType.String_Chart ) },
        };

        private static readonly Dictionary<string, IniModifierCreator> TICKRATE_LIST = new()
        {
            { "Resolution", new("Resolution", ModifierCreatorType.UInt16 ) },
        };

        private static YARGChart Load<TChar, TBase, TDecoder>(YARGTextReader<TChar, TDecoder> textReader, ParseSettings? parseSettings, Dictionary<NoteTracks_Chart, HashSet<Difficulty>>? activeTracks, bool doVocals)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
            
        {
            YARGChartFileReader<TChar, TDecoder, TBase> chartReader = new(textReader);
            if (!chartReader.ValidateHeaderTrack())
                throw new Exception("[Song] track expected at the start of the file");

            YARGChart chart = new();
            ParseHeaderTrack(chart, parseSettings == null ? MODIFIER_LIST : TICKRATE_LIST, chartReader);
            SetSustainThreshold(chart.Sync.Tickrate, parseSettings);

            DrumTrackHandler drums = new(parseSettings != null ? parseSettings.DrumsType : DrumsType.Unknown);
            while (chartReader.IsStartOfTrack())
            {
                if (chartReader.ValidateSyncTrack())
                    LoadSyncTrack(chart.Sync, chartReader);
                else if (chartReader.ValidateEventsTrack())
                    LoadEventsTrack(chart, chartReader, doVocals);
                else if (chartReader.ValidateDifficulty() && chartReader.ValidateInstrument() &&
                        (activeTracks == null ||
                        (activeTracks.TryGetValue(chartReader.Instrument, out var diffs) && diffs.Contains(chartReader.Difficulty))))
                {
                    if (chartReader.Instrument != NoteTracks_Chart.Drums)
                        SelectChartTrack(chart, chartReader);
                    else
                        drums.LoadChart(chart.Sync, chartReader);
                }
                else
                    chartReader.SkipTrack();
            }

            if (drums.IsOccupied())
            {
                var track = drums.GetChartTrack()!;
                switch (drums.Type)
                {
                    case DrumsType.ProDrums: chart.ProDrums =      (InstrumentTrack_FW<DrumNote<DrumPad_4, Pro_Drums>>) track; break;
                    case DrumsType.FiveLane: chart.FiveLaneDrums = (InstrumentTrack_FW<DrumNote<DrumPad_5, Basic_Drums>>) track; break;
                    case DrumsType.FourLane: chart.FourLaneDrums = (InstrumentTrack_FW<DrumNote<DrumPad_4, Basic_Drums>>) track; break;
                }
            }

            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static void SetSustainThreshold(uint tickrate, ParseSettings? settings)
        {
            DualTime.TruncationLimit = settings != null && settings.SustainCutoffThreshold != ParseSettings.SETTING_DEFAULT ? settings.SustainCutoffThreshold : 1;
        }

        private static void ParseHeaderTrack<TChar, TBase, TDecoder>(YARGChart chart, Dictionary<string, IniModifierCreator> list, YARGChartFileReader<TChar, TDecoder, TBase> chartReader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
        {
            var modifiers = chartReader.ExtractModifiers(list);
            if (modifiers.Remove("Resolution", out var tickrate))
            {
                chart.Sync.Tickrate = tickrate[0].UInt16;
                if (modifiers.Count == 0)
                    return;
            }

            //SongMetadata metadata = chart.metadata;
            //if (modifiers.Remove("Name", out var names))
            //{
            //    int i = 0;
            //    if (metadata.Name.Length == 0 || metadata.Name == DEFAULT_NAME)
            //    {
            //        metadata.Name = names[0].String;
            //        ++i;
            //    }

            //    while (chart.name != DEFAULT_NAME && i < names.Count)
            //        chart.name = names[i++].SORTSTR.Str;
            //}

            //if (modifiers.Remove("Artist", out var artists))
            //{
            //    int i = 0;
            //    if (chart.artist.Length == 0 || chart.artist == DEFAULT_ARTIST)
            //    {
            //        chart.artist = artists[0].STR;
            //        ++i;
            //    }

            //    while (chart.artist != DEFAULT_ARTIST && i < artists.Count)
            //        chart.artist = artists[i++].SORTSTR.Str;
            //}

            //if (modifiers.Remove("Album", out var albums))
            //{
            //    int i = 0;
            //    if (chart.album.Length == 0 || chart.album == DEFAULT_ALBUM)
            //    {
            //        chart.album = albums[0].STR;
            //        ++i;
            //    }

            //    while (chart.album != DEFAULT_ALBUM && i < albums.Count)
            //        chart.album = albums[i++].SORTSTR.Str;
            //}

            //if (modifiers.Remove("Genre", out var genres))
            //{
            //    int i = 0;
            //    if (chart.genre.Length == 0 || chart.genre == DEFAULT_GENRE)
            //    {
            //        chart.genre = genres[0].STR;
            //        ++i;
            //    }

            //    while (chart.genre != DEFAULT_GENRE && i < genres.Count)
            //        chart.genre = genres[i++].SORTSTR.Str;
            //}

            //if (modifiers.Remove("Year", out var years))
            //{
            //    int i = 0;
            //    if (chart.year.Length == 0 || chart.year == DEFAULT_YEAR)
            //    {
            //        chart.year = years[0].STR;
            //        ++i;
            //    }

            //    while (chart.year != DEFAULT_YEAR && i < years.Count)
            //        chart.year = years[i++].SORTSTR.Str;
            //}

            //if (modifiers.Remove("Charter", out var charters))
            //{
            //    int i = 0;
            //    if (chart.charter.Length == 0 || chart.charter == DEFAULT_CHARTER)
            //    {
            //        chart.charter = charters[0].STR;
            //        ++i;
            //    }

            //    while (chart.charter != DEFAULT_CHARTER && i < charters.Count)
            //        chart.charter = charters[i++].SORTSTR.Str;
            //}

            //foreach (var modifier in modifiers)
            //    if (!chart.miscellaneous.ContainsKey(modifier.Key))
            //        chart.miscellaneous.Add(modifier.Key, modifier.Value[0]);
        }

        private static void LoadSyncTrack<TChar, TBase, TDecoder>(SyncTrack_FW sync, YARGChartFileReader<TChar, TDecoder, TBase> chartReader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
        {
            DotChartEvent ev = default;
            while (chartReader.TryParseEvent(ref ev))
            {
                switch (ev.Type)
                {
                    case ChartEventType.Bpm:
                        sync.TempoMarkers.Get_Or_Add_Last(ev.Position).Micros = chartReader.ExtractMicrosPerQuarter();
                        break;
                    case ChartEventType.Anchor:
                        sync.TempoMarkers.Get_Or_Add_Last(ev.Position).Anchor = chartReader.ExtractAnchor();
                        break;
                    case ChartEventType.Time_Sig:
                        sync.TimeSigs.Get_Or_Add_Last(ev.Position) = chartReader.ExtractTimeSig();
                        break;
                }
                chartReader.NextEvent();
            }
            YARGChartFinalizer.FinalizeTempoMap(sync);
        }

        private static void LoadEventsTrack<TChar, TBase, TDecoder>(YARGChart chart, YARGChartFileReader<TChar, TDecoder, TBase> chartReader, bool doVocals)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TBase : unmanaged, IDotChartBases<TChar>
            where TDecoder : IStringDecoder<TChar>, new()
        {
            if (doVocals)
                chart.LeadVocals = new(1);

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            var phrase = DualTime.Inactive;

            DotChartEvent ev = default;
            while (chartReader.TryParseEvent(ref ev))
            {
                if (ev.Type == ChartEventType.Text)
                {
                    var dual = new DualTime(ev.Position, chart.Sync.ConvertToSeconds(ev.Position, ref tempoIndex));

                    string str = chartReader.ExtractText();
                    if (str.StartsWith(SECTION))
                        chart.Events.sections.Get_Or_Add_Last(dual) = str[8..];
                    else if (str.StartsWith(LYRIC))
                    {
                        if (doVocals)
                            chart.LeadVocals![0].Get_Or_Add_Last(dual).Lyric = str[6..];
                    }
                    else if (str == PHRASE_START)
                    {
                        if (doVocals)
                        {
                            if (phrase.ticks >= 0)
                                chart.LeadVocals!.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(dual - phrase));
                            phrase = dual;
                        }
                    }
                    else if (str == PHRASE_END)
                    {
                        // No need for doVocals check
                        if (phrase.ticks >= 0)
                        {
                            chart.LeadVocals!.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(dual - phrase));
                            phrase.ticks = -1;
                        }
                    }
                    else
                        chart.Events.globals.Get_Or_Add_Last(dual).Add(str);
                }
                chartReader.NextEvent();
            }
        }

        /// <remarks>Drums handled by dedicated class</remarks>
        private static void SelectChartTrack<TChar, TBase, TDecoder>(YARGChart chart, YARGChartFileReader<TChar, TDecoder, TBase> chartReader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
        {
            bool skip = chartReader.Instrument switch
            {
                NoteTracks_Chart.Single =>       LoadChartTrack(chartReader, chart.Sync, ref chart.FiveFretGuitar),
                NoteTracks_Chart.DoubleBass =>   LoadChartTrack(chartReader, chart.Sync, ref chart.FiveFretBass),
                NoteTracks_Chart.DoubleRhythm => LoadChartTrack(chartReader, chart.Sync, ref chart.FiveFretRhythm),
                NoteTracks_Chart.DoubleGuitar => LoadChartTrack(chartReader, chart.Sync, ref chart.FiveFretCoopGuitar),
                NoteTracks_Chart.GHLGuitar =>    LoadChartTrack(chartReader, chart.Sync, ref chart.SixFretGuitar),
                NoteTracks_Chart.GHLBass =>      LoadChartTrack(chartReader, chart.Sync, ref chart.SixFretBass),
                NoteTracks_Chart.GHLRhythm =>    LoadChartTrack(chartReader, chart.Sync, ref chart.SixFretRhythm),
                NoteTracks_Chart.GHLCoop =>      LoadChartTrack(chartReader, chart.Sync, ref chart.SixFretCoopGuitar),
                NoteTracks_Chart.Keys =>         LoadChartTrack(chartReader, chart.Sync, ref chart.Keys),
                _ => true,
            };

            if (skip)
                chartReader.SkipTrack();
        }

        private static bool LoadChartTrack<TChar, TBase, TDecoder, TNote>(YARGChartFileReader<TChar, TDecoder, TBase> chartReader, SyncTrack_FW sync, ref InstrumentTrack_FW<TNote>? track)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
            where TNote : unmanaged, INote, IDotChartLoadable
        {
            track ??= new InstrumentTrack_FW<TNote>();

            ref var difficultyTrack = ref track[chartReader.Difficulty];
            if (difficultyTrack != null)
                return true;

            difficultyTrack = new DifficultyTrack_FW<TNote>(5000);

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            List<DualTime> soloQueue = new(2);

            DotChartEvent ev = default;
            DotChartNote chartNote = default;
            while (chartReader.TryParseEvent(ref ev))
            {
                var dual = new DualTime(ev.Position, sync.ConvertToSeconds(ev.Position, ref tempoIndex));
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            ref var note = ref difficultyTrack.Notes.Get_Or_Add_Last(dual);
                            chartReader.ExtractLaneAndSustain(ref chartNote);

                            var dualDuration = new DualTime(chartNote.Duration, sync.ConvertToSeconds(chartNote.Duration, ref tempoIndex));
                            if (!note.SetFromDotChart(chartNote.Lane, dualDuration))
                                if (note.GetNumActiveNotes() == 0)
                                    difficultyTrack.Notes.Pop();
                            break;
                        }
                    case ChartEventType.Special:
                        {
                            (var type, long duration) = chartReader.ExtractSpecialPhrase();
                            switch (type)
                            {
                                case SpecialPhraseType.FaceOff_Player1:
                                case SpecialPhraseType.FaceOff_Player2:
                                case SpecialPhraseType.StarPower:
                                case SpecialPhraseType.BRE:
                                case SpecialPhraseType.Tremolo:
                                case SpecialPhraseType.Trill:
                                    var dualDuration = new DualTime(duration, sync.ConvertToSeconds(duration));
                                    difficultyTrack.SpecialPhrases.Get_Or_Add_Last(dual).TryAdd(type, new SpecialPhraseInfo(dualDuration));
                                    break;
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        {
                            string str = chartReader.ExtractText();
                            if (str.StartsWith(SOLOEND))
                            {
                                if (soloQueue.Count > 0)
                                {
                                    var solo = soloQueue[0];
                                    difficultyTrack.SpecialPhrases[solo].TryAdd(SpecialPhraseType.Solo, new SpecialPhraseInfo(dual - solo));
                                    soloQueue.RemoveAt(0);

                                    // Ensures overlapping soloes... don't overlap
                                    if (soloQueue.Count > 0 && soloQueue[0] != dual)
                                        soloQueue.Clear();
                                }
                            }
                            else if (str.StartsWith(SOLO))
                                soloQueue.Add(dual);
                            else
                                difficultyTrack.Events.Get_Or_Add_Last(dual).Add(str);
                            break;
                        }
                }
                chartReader.NextEvent();
            }

            if (soloQueue.Count > 0)
            {
                var solo = soloQueue[0];
                var dual = new DualTime(ev.Position, sync.ConvertToSeconds(ev.Position, ref tempoIndex));
                difficultyTrack.SpecialPhrases[solo].TryAdd(SpecialPhraseType.Solo, new SpecialPhraseInfo(dual - solo));
                soloQueue.RemoveAt(0);
            }

            difficultyTrack.TrimExcess();
            return false;
        }
    }
}
