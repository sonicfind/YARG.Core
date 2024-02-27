using System;
using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public static partial class YARGDotChartLoader
    {
        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";
        private const string SECTION = "section ";
        private const string LYRIC = "lyric ";
        private const string PHRASE_START = "phrase_start";
        private const string PHRASE_END = "phrase_end ";

        public static YARGChart Load(in FixedArray<byte> file, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<Instrument> activeTracks)
        {
            if (YARGTextReader.TryUTF8(file, out var byteContainer))
            {
                return Process(ref byteContainer, in metadata, in settings, drumsInChart, activeTracks);
            }

            using var chars = YARGTextReader.TryUTF16Cast(file);
            if (chars != null)
            {
                var charContainer = YARGTextReader.CreateUTF16Container(chars);
                return Process(ref charContainer, in metadata, in settings, drumsInChart, activeTracks);
            }

            using var ints = YARGTextReader.CastUTF32(file);
            var intContainer = YARGTextReader.CreateUTF32Container(ints);
            return Process(ref intContainer, in metadata, in settings, drumsInChart, activeTracks);
        }

        private static YARGChart Process<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<Instrument> activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.HEADERTRACK))
            {
                throw new Exception("[Song] track expected at the start of the file");
            }

            long tickrate = ParseTickrate(ref container);
            DualTime.SetTruncationLimit(settings, 1);

            var sync = new SyncTrack2(tickrate);
            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.SYNCTRACK))
            {
                FillSynctrack(ref container, sync);
            }

            var chart = new YARGChart(sync, metadata, settings);
            if (activeTracks.Contains(Instrument.Vocals))
            {
                chart.LeadVocals = new VocalTrack2(1);
            }

            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.EVENTTRACK))
                {
                    LoadEventsTrack(ref container, chart);
                }
                else if (!SelectChartTrack(ref container, chart, drumsInChart, activeTracks))
                {
                    if (YARGTextReader.SkipLinesUntil(ref container, TextConstants<TChar>.CLOSE_BRACE))
                    {
                        YARGTextReader.GotoNextLine(ref container);
                    }
                }
            }

            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static long ParseTickrate<TChar>(ref YARGTextContainer<TChar> container)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            var modifiers = YARGChartFileReader.ExtractModifiers(ref container);
            if (!modifiers.Extract("Resolution", out long tickrate) || tickrate <= 0)
            {
                tickrate = 192;
            }
            return tickrate;
        }

        private static void FillSynctrack<TChar>(ref YARGTextContainer<TChar> container, SyncTrack2 sync)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            DotChartEvent ev = default;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                switch (ev.Type)
                {
                    case ChartEventType.Bpm:
                        sync.TempoMarkers.GetLastOrAppend(ev.Position).MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                        break;
                    case ChartEventType.Anchor:
                        sync.TempoMarkers.GetLastOrAppend(ev.Position).Anchor = ev.Position > 0 ? YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container) : 0;
                        break;
                    case ChartEventType.Time_Sig:
                        sync.TimeSigs.GetLastOrAppend(ev.Position) = YARGChartFileReader.ExtractTimeSig(ref container);
                        break;
                }
                YARGTextReader.GotoNextLine(ref container);
            }
            YARGChartFinalizer.FinalizeAnchors(sync);
        }

        private static void LoadEventsTrack<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            var phrase = DualTime.Inactive;

            DotChartEvent ev = default;
            DualTime position = default;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Text)
                {
                    position.Ticks = ev.Position;
                    position.Seconds = chart.Sync.ConvertToSeconds(ev.Position, ref tempoIndex);

                    string str = YARGTextReader.ExtractText(ref container, false);
                    if (str.StartsWith(SECTION))
                    {
                        chart.Events.Sections.GetLastOrAppend(position) = str[SECTION.Length..];
                    }
                    else if (str.StartsWith(LYRIC))
                    {
                        if (chart.LeadVocals != null)
                        {
                            chart.LeadVocals[0].GetLastOrAppend(position).Lyric = str[LYRIC.Length..];
                        }
                    }
                    else if (str == PHRASE_START)
                    {
                        if (chart.LeadVocals != null)
                        {
                            if (phrase.Ticks >= 0)
                            {
                                chart.LeadVocals.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(position - phrase));
                            }
                            phrase = position;
                        }
                    }
                    else if (str == PHRASE_END)
                    {
                        // No need for doVocals check
                        if (phrase.Ticks >= 0)
                        {
                            chart.LeadVocals!.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(position - phrase));
                            phrase.Ticks = -1;
                        }
                    }
                    else
                    {
                        chart.Events.Globals.GetLastOrAppend(position).Add(str);
                    }
                }
                YARGTextReader.GotoNextLine(ref container);
            }

            if (chart.LeadVocals != null && !chart.LeadVocals.IsOccupied())
            {
                chart.LeadVocals = null;
            }
        }

        private static bool SelectChartTrack<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, DrumsType drumsInChart, HashSet<Instrument> activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!YARGChartFileReader.ValidateInstrument(ref container, out var instrument, out var difficulty))
            {
                return false;
            }

            // The note track label for drums will only return the
            // four lanes enum value
            //
            // We expect to the `activeTracks` parameter to hold the matching
            // drums instrument value to `drumsInChart` if drums are active
            if (instrument == Instrument.FourLaneDrums)
            {
                if (drumsInChart == DrumsType.ProDrums)
                {
                    instrument = Instrument.ProDrums;
                }
                else if (drumsInChart == DrumsType.FiveLane)
                {
                    instrument = Instrument.FiveLaneDrums;
                }
            }

            if (!activeTracks.Contains(instrument))
            {
                return false;
            }

            unsafe
            {
                return instrument switch
                {
                    Instrument.FiveFretGuitar =>     LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretGuitar, &Set),
                    Instrument.FiveFretBass =>       LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretBass, &Set),
                    Instrument.FiveFretRhythm =>     LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretRhythm, &Set),
                    Instrument.FiveFretCoopGuitar => LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretCoopGuitar, &Set),
                    Instrument.SixFretGuitar =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretGuitar, &Set),
                    Instrument.SixFretBass =>        LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretBass, &Set),
                    Instrument.SixFretRhythm =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretRhythm, &Set),
                    Instrument.SixFretCoopGuitar =>  LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretCoopGuitar, &Set),
                    Instrument.FourLaneDrums =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FourLaneDrums, &Set),
                    Instrument.ProDrums =>           LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.ProDrums, &Set),
                    Instrument.FiveLaneDrums =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveLaneDrums, &Set),
                    Instrument.Keys =>               LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.Keys, &Set),
                    _ => false,
                };
            }
        }

        private static unsafe bool LoadChartTrack<TChar, TNote>(ref YARGTextContainer<TChar> container, SyncTrack2 sync, Difficulty difficulty, ref BasicInstrumentTrack2<TNote>? track, delegate*<ref TNote, int, DualTime, bool> loader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TNote : unmanaged, IInstrumentNote
        {
            track ??= new BasicInstrumentTrack2<TNote>();

            ref var difficultyTrack = ref track[difficulty];
            if (difficultyTrack != null)
            {
                return false;
            }

            difficultyTrack = new DifficultyTrack2<TNote>(5000);

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;

            DotChartEvent ev = default;
            DualTime position = default;
            DualTime duration = default;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                position.Ticks = ev.Position;
                position.Seconds = sync.ConvertToSeconds(ev.Position, ref tempoIndex);
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            ref var note = ref difficultyTrack.Notes.GetLastOrAppend(position);

                            int lane = YARGChartFileReader.ExtractWithWhitespace<TChar, int>(ref container);
                            long tickDuration = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);

                            duration.Ticks = tickDuration;
                            duration.Seconds = sync.ConvertToSeconds(tickDuration, tempoIndex);
                            if (!loader(ref note, lane, duration))
                            {
                                if (note.GetNumActiveLanes() == 0)
                                {
                                    difficultyTrack.Notes.Pop();
                                }
                            }
                            break;
                        }
                    case ChartEventType.Special:
                        {
                            var type = (SpecialPhraseType)YARGChartFileReader.ExtractWithWhitespace<TChar, int>(ref container);
                            long tickDuration = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);
                            switch (type)
                            {
                                case SpecialPhraseType.FaceOff_Player1:
                                case SpecialPhraseType.FaceOff_Player2:
                                case SpecialPhraseType.StarPower:
                                case SpecialPhraseType.BRE:
                                case SpecialPhraseType.Tremolo:
                                case SpecialPhraseType.Trill:
                                    duration.Ticks = tickDuration;
                                    duration.Seconds = sync.ConvertToSeconds(tickDuration, tempoIndex);
                                    difficultyTrack.SpecialPhrases.GetLastOrAppend(position).TryAdd(type, new SpecialPhraseInfo(duration));
                                    break;
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        string str = YARGTextReader.ExtractText(ref container, false);
                        difficultyTrack.Events.GetLastOrAppend(position).Add(str);
                        break;
                }
                YARGTextReader.GotoNextLine(ref container);
            }

            for (int i = 0; i < difficultyTrack.Events.Count;)
            {
                var start = difficultyTrack.Events.ElementAtIndex(i);
                if (!start.Value.Remove(SOLO))
                {
                    ++i;
                    continue;
                }

                if (start.Value.Count == 0)
                {
                    difficultyTrack.Events.RemoveAtIndex(i);
                }

                while (i < difficultyTrack.Events.Count)
                {
                    var end = difficultyTrack.Events.ElementAtIndex(i);
                    if (end.Value.Remove(SOLOEND))
                    {
                        var info = new SpecialPhraseInfo(end.Key - start.Key);
                        difficultyTrack.SpecialPhrases[start.Key].TryAdd(SpecialPhraseType.Solo, info);

                        if (end.Value.Count == 0)
                        {
                            difficultyTrack.Events.RemoveAtIndex(i);
                        }
                        break;
                    }
                    ++i;
                }
            }

            difficultyTrack.TrimExcess();
            return false;
        }
    }
}
