using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Logging;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";
        private const string SECTION = "section ";
        private const string LYRIC = "lyric ";
        private const string PHRASE_START = "phrase_start";
        private const string PHRASE_END = "phrase_end";

        public static YARGChart LoadChart(string chartPath, HashSet<Instrument>? activeTracks)
        {
            var iniPath = Path.Combine(Path.GetDirectoryName(chartPath), "song.ini");

            IniModifierCollection modifiers;
            var drumsType = DrumsType.Any;
            if (File.Exists(iniPath))
            {
                modifiers = SongIniHandler.ReadSongIniFile(iniPath);
                if (modifiers.Extract("five_lane_drums", out bool fiveLane))
                {
                    drumsType = fiveLane ? DrumsType.FiveLane : DrumsType.FourOrPro;
                }

                if (modifiers.Extract("proDrums", out bool proDrums) && drumsType != DrumsType.FiveLane)
                {
                    // We don't want to just immediately set the value to one or the other
                    // on the chance that we still need to test for FiveLane.
                    // We just know what the .ini explicitly tells us it *isn't*
                    if (proDrums)
                    {
                        drumsType -= DrumsType.FourLane;
                    }
                    else
                    {
                        drumsType -= DrumsType.ProDrums;
                    }
                }
            }
            else
            {
                modifiers = new IniModifierCollection();
            }

            using var bytes = FixedArray.LoadFile(chartPath);
            var chart = LoadChart(bytes, in SongMetadata.Default, in LoaderSettings.Default, modifiers, drumsType, activeTracks);
            if (!modifiers.Extract("hopo_frequency", out chart.Settings.HopoThreshold) || chart.Settings.HopoThreshold <= 0)
            {
                if (modifiers.Extract("eighthnote_hopo", out bool eighthNoteHopo))
                {
                    chart.Settings.HopoThreshold = chart.Resolution / (eighthNoteHopo ? 2 : 3);
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
                    chart.Settings.HopoThreshold = 4 * chart.Resolution / denominator;
                }
                else
                {
                    chart.Settings.HopoThreshold = chart.Resolution / 3;
                }

                // With a 192 resolution, .chart has a HOPO threshold of 65 ticks, not 64,
                // so we need to scale this factor to different resolutions (480 res = 162.5 threshold).
                // Why?... idk, but I hate it.
                const float DEFAULT_RESOLUTION = 192;
                chart.Settings.HopoThreshold += (long) (chart.Resolution / DEFAULT_RESOLUTION);
            }

            // .chart defaults to no cutting off sustains whatsoever if the ini does not define the value.
            // Since a failed `TryGet` sets the value to zero, we would need no additional work
            modifiers.Extract("sustain_cutoff_threshold", out chart.Settings.SustainCutoffThreshold);
            return chart;
        }

        public static YARGChart LoadChart(FixedArray<byte> file, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
        {
            YARGChart chart;
            if (YARGTextReader.TryUTF8(file, out var byteContainer))
            {
                chart = Initialize_Chart(ref byteContainer, in metadata, in settings, modifiers);
                LoadTracks_Chart(chart, ref byteContainer, drumsInChart, activeTracks);
            }
            else
            {
                using var chars = YARGTextReader.TryUTF16Cast(file);
                if (chars != null)
                {
                    var charContainer = YARGTextReader.CreateUTF16Container(chars);
                    chart = Initialize_Chart(ref charContainer, in metadata, in settings, modifiers);
                    LoadTracks_Chart(chart, ref charContainer, drumsInChart, activeTracks);
                }
                else
                {
                    using var ints = YARGTextReader.CastUTF32(file);
                    var intContainer = YARGTextReader.CreateUTF32Container(ints);
                    chart = Initialize_Chart(ref intContainer, in metadata, in settings, modifiers);
                }
            }
            return chart;
        }

        private static YARGChart Initialize_Chart<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? miscellaneous)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            const long DEFAULT_TICKRATE = 192;
            if (!YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.HEADERTRACK))
            {
                throw new Exception("[Song] track expected at the start of the file");
            }

            var modifiers = YARGChartFileReader.ExtractModifiers(ref container);
            if (!modifiers.Extract("Resolution", out long tickrate) || tickrate <= 0)
            {
                tickrate = DEFAULT_TICKRATE;
            }

            var chart = new YARGChart(tickrate, in metadata, in settings, miscellaneous);
            if (chart.Miscellaneous != null)
            {
                chart.Miscellaneous.Union(modifiers);
                SongMetadata.FillFromIni(ref chart.Metadata, chart.Miscellaneous);
            }

            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.SYNCTRACK))
            {
                DotChartEvent ev = default;
                while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
                {
                    unsafe
                    {
                        switch (ev.Type)
                        {
                            case ChartEventType.Bpm:
                                chart.Sync.TempoMarkers.GetLastOrAdd(ev.Position)->MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                                break;
                            case ChartEventType.Anchor:
                                if (ev.Position > 0)
                                {
                                    chart.Sync.TempoMarkers.GetLastOrAdd(ev.Position)->Anchor = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);
                                }
                                break;
                            case ChartEventType.Time_Sig:
                                chart.Sync.TimeSigs.AddOrUpdate(ev.Position, YARGChartFileReader.ExtractTimeSig(ref container));
                                break;
                        }
                    }
                }
                FinalizeAnchors(chart.Sync, chart._resolution);
            }
            return chart;
        }

        private static void LoadTracks_Chart<TChar>(YARGChart chart, ref YARGTextContainer<TChar> container, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            InstrumentTrack2<UnknownLaneDrums>? unknownDrums = null;
            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                unsafe
                {
                    if (!SelectTrack_Chart(ref container, chart, &drumsInChart, activeTracks, ref unknownDrums))
                    {
                        YARGChartFileReader.SkipToNextTrack(ref container);
                    }
                }
            }

            if (unknownDrums != null)
            {
                if ((drumsInChart & DrumsType.FourOrPro) > 0)
                {
                    unknownDrums.Convert(chart.FourLaneDrums);
                }
                else
                {
                    unknownDrums.Convert(chart.FiveLaneDrums);
                }
            }
            FinalizeDeserialization(chart);
        }

        private static bool LoadEventsTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty() || !chart.LeadVocals.IsEmpty())
            {
                YargLogger.LogInfo("[Events] track appears multiple times. Not parsing repeats...");
                return false;
            }

            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);
            var ev = default(DotChartEvent);
            var position = default(DualTime);
            var phrase = DualTime.Inactive;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Text)
                {
                    position.Ticks = ev.Position;
                    position.Seconds = tempoTracker.Traverse(ev.Position);

                    string str = YARGTextReader.ExtractText(ref container, true);
                    if (str.StartsWith(SECTION))
                    {
                        chart.Sections.AddOrUpdate(in position, str[SECTION.Length..]);
                    }
                    else if (str.StartsWith(LYRIC))
                    {
                        chart.LeadVocals[0].Lyrics.AddOrUpdate(in position, str[LYRIC.Length..]);
                    }
                    else if (str == PHRASE_START)
                    {
                        if (phrase.Ticks >= 0 && position.Ticks > phrase.Ticks)
                        {
                            chart.LeadVocals.VocalPhrases_1.Add(in phrase, position - phrase);
                        }
                        phrase = position;
                    }
                    else if (str == PHRASE_END)
                    {
                        if (phrase.Ticks >= 0)
                        {
                            if (position.Ticks > phrase.Ticks)
                            {
                                chart.LeadVocals!.VocalPhrases_1.Add(in phrase, position - phrase);
                            }
                            phrase.Ticks = -1;
                        }
                    }
                    else
                    {
                        chart.Globals.GetLastOrAdd(in position).Add(str);
                    }
                }
            }
            return true;
        }

        private static unsafe bool SelectTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, DrumsType* drumsInChart, HashSet<Instrument>? activeTracks, ref InstrumentTrack2<UnknownLaneDrums>? unknownDrums)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.EVENTTRACK))
            {
                return LoadEventsTrack_Chart(ref container, chart);
            }

            if (!YARGChartFileReader.ValidateInstrument(ref container, out var instrument, out var difficulty))
            {
                return false;
            }

            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);

            // The note track label for drums will only return the
            // four lanes enum value
            //
            // We expect to the `activeTracks` parameter to hold the matching
            // drums instrument value to `drumsInChart` if drums are active
            if (instrument == Instrument.FourLaneDrums)
            {
                switch (*drumsInChart)
                {
                case DrumsType.ProDrums: instrument = Instrument.ProDrums; break;
                case DrumsType.FiveLane: instrument = Instrument.FiveLaneDrums; break;
                case DrumsType.FourLane: break;
                default:
                    if (activeTracks != null)
                    {
                        return false;
                    }
                    unknownDrums ??= new InstrumentTrack2<UnknownLaneDrums>();
                    return LoadInstrumentTrack_Chart(ref container, unknownDrums[difficulty], ref tempoTracker, new UnknownLaneSetter(drumsInChart));
                }
            }

            if (activeTracks != null && !activeTracks.Contains(instrument))
            {
                return false;
            }

            return instrument switch
            {
                Instrument.FiveFretGuitar =>     LoadInstrumentTrack_Chart(ref container, chart.FiveFretGuitar    [difficulty], ref tempoTracker, new FiveFretSetter()),
                Instrument.FiveFretBass =>       LoadInstrumentTrack_Chart(ref container, chart.FiveFretBass      [difficulty], ref tempoTracker, new FiveFretSetter()),
                Instrument.FiveFretRhythm =>     LoadInstrumentTrack_Chart(ref container, chart.FiveFretRhythm    [difficulty], ref tempoTracker, new FiveFretSetter()),
                Instrument.FiveFretCoopGuitar => LoadInstrumentTrack_Chart(ref container, chart.FiveFretCoopGuitar[difficulty], ref tempoTracker, new FiveFretSetter()),
                Instrument.Keys =>               LoadInstrumentTrack_Chart(ref container, chart.Keys              [difficulty], ref tempoTracker, new FiveFretSetter()),
                Instrument.SixFretGuitar =>      LoadInstrumentTrack_Chart(ref container, chart.SixFretGuitar     [difficulty], ref tempoTracker, new SixFretSetter()),
                Instrument.SixFretBass =>        LoadInstrumentTrack_Chart(ref container, chart.SixFretBass       [difficulty], ref tempoTracker, new SixFretSetter()),
                Instrument.SixFretRhythm =>      LoadInstrumentTrack_Chart(ref container, chart.SixFretRhythm     [difficulty], ref tempoTracker, new SixFretSetter()),
                Instrument.SixFretCoopGuitar =>  LoadInstrumentTrack_Chart(ref container, chart.SixFretCoopGuitar [difficulty], ref tempoTracker, new SixFretSetter()),
                Instrument.FourLaneDrums =>      LoadInstrumentTrack_Chart(ref container, chart.FourLaneDrums     [difficulty], ref tempoTracker, new FourLaneSetter()),
                Instrument.ProDrums =>           LoadInstrumentTrack_Chart(ref container, chart.FourLaneDrums     [difficulty], ref tempoTracker, new ProDrumsSetter()),
                Instrument.FiveLaneDrums =>      LoadInstrumentTrack_Chart(ref container, chart.FiveLaneDrums     [difficulty], ref tempoTracker, new FiveLaneSetter()),
                _ => false,
            };
        }

        private enum SpecialPhraseType
        {
            FaceOff_Player1 = 0,
            FaceOff_Player2 = 1,
            StarPower = 2,
            BRE = 64,
            Tremolo = 65,
            Trill = 66,
        }

        private static bool LoadInstrumentTrack_Chart<TChar, TNote, TSetter>(ref YARGTextContainer<TChar> container, DifficultyTrack2<TNote> difficultyTrack, ref TempoTracker tempoTracker, TSetter setter)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TNote : unmanaged, IInstrumentNote
            where TSetter : unmanaged, IChartLoadable<TNote>
        {
            if (!difficultyTrack.IsEmpty())
            {
                return false;
            }
            difficultyTrack.Notes.Capacity = 5000;

            // Keeps tracks of soloes that start on the same tick when another solo ends
            var soloPosition = DualTime.Inactive;
            var nextSoloPosition = DualTime.Inactive;

            var ev = default(DotChartEvent);
            var position = default(DualTime);
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                position.Ticks = ev.Position;
                position.Seconds = tempoTracker.Traverse(ev.Position);

                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        unsafe
                        {
                            var (lane, duration) = YARGChartFileReader.ExtractLaneAndDuration(ref container, in position, in tempoTracker);
                            var note = difficultyTrack.Notes.GetLastOrAdd(in position);
                            if (!setter.Set(note, lane, in duration) && note->GetNumActiveLanes() == 0)
                            {
                                difficultyTrack.Notes.Pop();
                            }
                            break;
                        }
                    case ChartEventType.Special:
                        {
                            var (lane, duration) = YARGChartFileReader.ExtractLaneAndDuration(ref container, in position, in tempoTracker);
                            switch ((SpecialPhraseType) lane)
                            {
                                case SpecialPhraseType.FaceOff_Player1: AddSpecialPhrase(difficultyTrack.Faceoff_Player1, in position, in duration); break;
                                case SpecialPhraseType.FaceOff_Player2: AddSpecialPhrase(difficultyTrack.Faceoff_Player2, in position, in duration); break;
                                case SpecialPhraseType.StarPower:       AddSpecialPhrase(difficultyTrack.Overdrives,      in position, in duration); break;
                                case SpecialPhraseType.BRE:             AddSpecialPhrase(difficultyTrack.BREs,            in position, in duration); break;
                                case SpecialPhraseType.Tremolo:         AddSpecialPhrase(difficultyTrack.Tremolos,        in position, in duration); break;
                                case SpecialPhraseType.Trill:           AddSpecialPhrase(difficultyTrack.Trills,          in position, in duration); break;
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        string str = YARGTextReader.ExtractText(ref container, true);
                        if (str == SOLO)
                        {
                            if (soloPosition.Ticks == -1)
                            {
                                soloPosition = position;
                            }
                            else
                            {
                                nextSoloPosition = position;
                            }
                        }
                        else if (str == SOLOEND)
                        {
                            if (soloPosition.Ticks != -1)
                            {
                                // .chart handles solo phrases with *inclusive ends*, so we have to add one tick.
                                // The only exception will be if another solo starts on the same exact tick.
                                //
                                // Comparing to the current tick instead of against uint.MaxValue ensures
                                // that the we don't allow overlaps
                                if (nextSoloPosition != position)
                                {
                                    ++position.Ticks;
                                    position.Seconds = tempoTracker.UnmovingConvert(position.Ticks);
                                    difficultyTrack.Soloes.Add(in soloPosition, position - soloPosition);
                                    soloPosition = DualTime.Inactive;
                                }
                                else
                                {
                                    difficultyTrack.Soloes.Add(in soloPosition, nextSoloPosition - soloPosition);
                                    soloPosition = nextSoloPosition;
                                    nextSoloPosition = DualTime.Inactive;
                                }
                            }
                        }
                        else 
                        {
                            difficultyTrack.Events.GetLastOrAdd(in position).Add(str);
                        }
                        break;
                }
            }
            return true;
        }

        private static unsafe void AddSpecialPhrase(YARGNativeSortedList<DualTime, DualTime> phrases, in DualTime position, in DualTime duration)
        {
            if (phrases.Count > 0)
            {
                ref var last = ref phrases.Data[phrases.Count - 1];
                if (last.Key + last.Value > position)
                {
                    last.Value = position - last.Key;
                }
            }
            phrases.Add(in position, duration);
        }

        private interface IChartLoadable<TNote>
            where TNote : unmanaged, IInstrumentNote
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(TNote* note, int lane, in DualTime length);
        }

        private readonly struct FiveFretSetter : IChartLoadable<FiveFretGuitar>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(FiveFretGuitar* note, int lane, in DualTime length)
            {
                switch (lane)
                {
                    case 0: note->Green = length; break;
                    case 1: note->Red = length; break;
                    case 2: note->Yellow = length; break;
                    case 3: note->Blue = length; break;
                    case 4: note->Orange = length; break;
                    case 5:
                        if (note->State == GuitarState.Natural)
                        {
                            note->State = GuitarState.Forced;
                        }
                        break;
                    case 6: note->State = GuitarState.Tap; break;
                    case 7: note->Open = length; break;
                    default:
                        return false;
                }
                return true;
            }
        }

        private readonly struct SixFretSetter : IChartLoadable<SixFretGuitar>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(SixFretGuitar* note, int lane, in DualTime length)
            {
                switch (lane)
                {
                    case 0: note->White1 = length; break;
                    case 1: note->White2 = length; break;
                    case 2: note->White3 = length; break;
                    case 3: note->Black1 = length; break;
                    case 4: note->Black2 = length; break;
                    case 5:
                        if (note->State == GuitarState.Natural)
                        {
                            note->State = GuitarState.Forced;
                        }
                        break;
                    case 6: note->State = GuitarState.Tap; break;
                    case 7: note->Open = length; break;
                    case 8: note->Black3 = length; break;
                    default:
                        return false;
                }
                return true;
            }
        }

        private readonly struct FourLaneSetter : IChartLoadable<FourLaneDrums>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(FourLaneDrums* note, int lane, in DualTime length)
            {
                switch (lane)
                {
                    case 0: note->Kick = length; break;
                    case 1: note->Snare = length; break;
                    case 2: note->Yellow = length; break;
                    case 3: note->Blue = length; break;
                    case 4: note->Green = length; break;

                    case 32: note->KickState = KickState.PlusOnly; break;

                    case 34: note->Dynamics_Snare = DrumDynamics.Accent; break;
                    case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                    case 36: note->Dynamics_Blue = DrumDynamics.Accent; break;
                    case 37: note->Dynamics_Green = DrumDynamics.Accent; break;

                    case 40: note->Dynamics_Snare = DrumDynamics.Ghost; break;
                    case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                    case 42: note->Dynamics_Blue = DrumDynamics.Ghost; break;
                    case 43: note->Dynamics_Green = DrumDynamics.Ghost; break;
                    default:
                        return false;
                }
                return true;
            }
        }

        private readonly struct ProDrumsSetter : IChartLoadable<FourLaneDrums>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(FourLaneDrums* note, int lane, in DualTime length)
            {
                switch (lane)
                {
                    case 0: note->Kick = length; break;
                    case 1: note->Snare = length; break;
                    case 2: note->Yellow = length; break;
                    case 3: note->Blue = length; break;
                    case 4: note->Green = length; break;

                    case 32: note->KickState = KickState.PlusOnly; break;

                    case 34: note->Dynamics_Snare = DrumDynamics.Accent; break;
                    case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                    case 36: note->Dynamics_Blue = DrumDynamics.Accent; break;
                    case 37: note->Dynamics_Green = DrumDynamics.Accent; break;

                    case 40: note->Dynamics_Snare = DrumDynamics.Ghost; break;
                    case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                    case 42: note->Dynamics_Blue = DrumDynamics.Ghost; break;
                    case 43: note->Dynamics_Green = DrumDynamics.Ghost; break;

                    case 66: note->Cymbal_Yellow = true; break;
                    case 67: note->Cymbal_Blue = true; break;
                    case 68: note->Cymbal_Green = true; break;
                    default:
                        return false;
                }
                return true;
            }
        }

        private readonly struct FiveLaneSetter : IChartLoadable<FiveLaneDrums>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(FiveLaneDrums* note, int lane, in DualTime length)
            {
                switch (lane)
                {
                    case 0: note->Kick = length; break;
                    case 1: note->Snare = length; break;
                    case 2: note->Yellow = length; break;
                    case 3: note->Blue = length; break;
                    case 4: note->Orange = length; break;
                    case 5: note->Green = length; break;

                    case 32: note->KickState = KickState.PlusOnly; break;

                    case 34: note->Dynamics_Snare = DrumDynamics.Accent; break;
                    case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                    case 36: note->Dynamics_Blue = DrumDynamics.Accent; break;
                    case 37: note->Dynamics_Orange = DrumDynamics.Accent; break;
                    case 38: note->Dynamics_Green = DrumDynamics.Accent; break;

                    case 40: note->Dynamics_Snare = DrumDynamics.Ghost; break;
                    case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                    case 42: note->Dynamics_Blue = DrumDynamics.Ghost; break;
                    case 43: note->Dynamics_Orange = DrumDynamics.Ghost; break;
                    case 44: note->Dynamics_Green = DrumDynamics.Ghost; break;
                    default:
                        return false;
                }
                return true;
            }
        }

        private readonly struct UnknownLaneSetter : IChartLoadable<UnknownLaneDrums>
        {
            private readonly unsafe DrumsType* _type;

            public unsafe UnknownLaneSetter(DrumsType* type)
            {
                _type = type;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Set(UnknownLaneDrums* note, int lane, in DualTime length)
            {
                switch (lane)
                {
                    case 0: note->Kick = length; break;
                    case 1: note->Snare = length; break;
                    case 2: note->Yellow = length; break;
                    case 3: note->Blue = length; break;
                    case 4: note->Orange = length; break;
                    case 5:
                        if (!_type->Has(DrumsType.FiveLane))
                        {
                            return false;
                        }
                        note->Green = length;
                        *_type = DrumsType.FiveLane;
                        break;
                    case 32: note->KickState = KickState.PlusOnly; break;

                    case 34: note->Dynamics_Snare = DrumDynamics.Accent; break;
                    case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                    case 36: note->Dynamics_Blue = DrumDynamics.Accent; break;
                    case 37: note->Dynamics_Orange = DrumDynamics.Accent; break;
                    case 38: note->Dynamics_Green = DrumDynamics.Accent; break;

                    case 40: note->Dynamics_Snare = DrumDynamics.Ghost; break;
                    case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                    case 42: note->Dynamics_Blue = DrumDynamics.Ghost; break;
                    case 43: note->Dynamics_Orange = DrumDynamics.Ghost; break;
                    case 44: note->Dynamics_Green = DrumDynamics.Ghost; break;

                    case 66:
                    case 67:
                    case 68:
                        if (!_type->Has(DrumsType.ProDrums))
                        {
                            return false;
                        }
                        (&note->Cymbal_Yellow)[lane - 66] = true;
                        *_type = DrumsType.ProDrums;
                        break;
                    default:
                        return false;
                }
                return true;
            }
        }
    }
}
