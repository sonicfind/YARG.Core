using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    internal class YARGChartFinalizer
    {
        public static void FinalizeAnchors(SyncTrack2 sync)
        {
            unsafe
            {
                var end = sync.TempoMarkers.End;
                // We can skip the first Anchor, even if not explicitly set (as it'd still be 0)
                for (var marker = sync.TempoMarkers.Data + 1; marker < end; ++marker)
                {
                    if (marker->Value.Anchor == 0)
                    {
                        var prev = marker - 1;
                        marker->Value.Anchor = (long) (((marker->Key - prev->Key) / (float) sync.Tickrate) * prev->Value.MicrosPerQuarter) + prev->Value.Anchor;
                    }
                }
            }
        }

        public static (DualTime Position, bool IsEndMarker) GetEndTime(YARGChart chart)
        {
            var globals = chart.Events.Globals.Span;
            for (int i = globals.Length - 1; i >= 0; --i)
            {
                foreach (string ev in globals[i].Value)
                {
                    if (ev == "[end]")
                    {
                        return (globals[i].Key, true);
                    }
                }
            }

            Track?[] tracks =
            {
                chart.FiveFretGuitar,
                chart.FiveFretBass,
                chart.FiveFretRhythm,
                chart.FiveFretCoopGuitar,
                chart.SixFretGuitar,
                chart.SixFretBass,
                chart.SixFretRhythm,
                chart.SixFretCoopGuitar,

                chart.Keys,

                chart.FourLaneDrums,
                chart.ProDrums,
                chart.FiveLaneDrums,

                //chart.TrueDrums,

                chart.ProGuitar_17Fret,
                chart.ProGuitar_22Fret,
                chart.ProBass_17Fret,
                chart.ProBass_22Fret,

                chart.ProKeys,

                //chart.Dj,

                chart.LeadVocals,
                chart.HarmonyVocals,
            };

            DualTime lastNoteTime = default;
            foreach (var track in tracks)
            {
                if (track != null)
                {
                    var lastTime = track.GetLastNoteTime();
                    if (lastTime > lastNoteTime)
                        lastNoteTime = lastTime;
                }
            }
            return (lastNoteTime, false);
        }

        public static void FinalizeBeats(YARGChart chart)
        {
            if (chart.BeatMap.IsEmpty())
            {
                GenerateAllBeats(chart);
            }
            else
            {
                GenerateLeftoverBeats(chart);
            }
        }

        private static void GenerateLeftoverBeats(YARGChart chart)
        {
            var (endPosition, isEndMarker) = GetEndTime(chart);

            var sync = chart.Sync;
            var beats = chart.BeatMap;
            long multipliedTickrate = 4 * sync.Tickrate;

            int beatIndex = 0;
            int tempoIndex = 0;

            DualTime buffer = default;
            unsafe
            {
                var end = sync.TimeSigs.End;
                for (var currSig = sync.TimeSigs.Data; currSig < end; ++currSig)
                {
                    long ticksPerMarker = multipliedTickrate >> currSig->Value.Denominator;
                    long ticksPerMeasure = (multipliedTickrate * currSig->Value.Numerator) >> currSig->Value.Denominator;

                    long endTime;
                    if (currSig + 1 < end)
                    {
                        endTime = currSig[1].Key;
                    }
                    else
                    {
                        endTime = endPosition.Ticks;
                        if (!isEndMarker)
                        {
                            long tickDisplacement = endTime - currSig->Key;
                            long mod = tickDisplacement % ticksPerMeasure;
                            if (mod > 0)
                            {
                                endTime += ticksPerMeasure - mod;
                            }
                        }
                    }

                    while (currSig->Key < endTime)
                    {
                        long position = currSig->Key;
                        for (uint n = 0; n < currSig->Value.Numerator && position < endTime; ++n)
                        {
                            while (beatIndex < beats.Count && beats.ElementAtIndex(beatIndex).Key.Ticks < position)
                            {
                                ++beatIndex;
                            }

                            if (beatIndex == beats.Count || position < beats.ElementAtIndex(beatIndex).Key.Ticks)
                            {
                                buffer.Ticks = position;
                                buffer.Seconds = sync.ConvertToSeconds(position, ref tempoIndex);
                                beats.Insert_Forced(beatIndex, buffer, BeatlineType.Weak);
                            }
                            ++beatIndex;
                            position += ticksPerMarker;
                        }
                        currSig->Key += ticksPerMeasure;
                    }

                    if (currSig + 1 == end)
                    {
                        buffer.Ticks = endTime;
                        buffer.Seconds = sync.ConvertToSeconds(endTime, ref tempoIndex);
                        beats.Append(buffer, BeatlineType.Measure);
                    }
                }
            }
        }

        private static void GenerateAllBeats(YARGChart chart)
        {
            var (endPosition, isEndMarker) = GetEndTime(chart);

            var sync = chart.Sync;
            var beats = chart.BeatMap;
            long multipliedTickrate = 4 * sync.Tickrate;

            int tempoIndex = 0;
            DualTime buffer = default;
            unsafe
            {
                var end = sync.TimeSigs.End;
                for (var currSig = sync.TimeSigs.Data; currSig < end; ++currSig)
                {
                    int numerator = currSig->Value.Numerator;
                    int markersPerClick = (6 << currSig->Value.Denominator) / currSig->Value.Metronome;
                    long ticksPerMarker = multipliedTickrate >> currSig->Value.Denominator;
                    long ticksPerMeasure = (multipliedTickrate * numerator) >> currSig->Value.Denominator;
                    bool isIrregular = numerator > 4 || (numerator & 1) == 1;

                    long endTime;
                    if (currSig + 1 < end)
                    {
                        endTime = currSig[1].Key;
                    }
                    else
                    {
                        endTime = endPosition.Ticks;
                        if (!isEndMarker)
                        {
                            long tickDisplacement = endTime - currSig->Key;
                            long mod = tickDisplacement % ticksPerMeasure;
                            if (mod > 0)
                            {
                                endTime += ticksPerMeasure - mod;
                            }
                        }
                    }

                    while (currSig->Key < endTime)
                    {
                        var style = BeatlineType.Measure;
                        long position = currSig->Key;
                        int clickSpacing = markersPerClick;
                        int triplSpacing = 3 * markersPerClick;

                        for (int count = 0, clickCount = 0; count < numerator && position < endTime; ++count)
                        {
                            buffer.Ticks = position;
                            buffer.Seconds = sync.ConvertToSeconds(position, ref tempoIndex);
                            beats.Append(buffer, style);

                            position += ticksPerMarker;
                            ++clickCount;
                            if (clickCount < clickSpacing)
                            {
                                style = BeatlineType.Weak;
                            }
                            else
                            {
                                clickCount = 0;
                                style = BeatlineType.Strong;
                                if (isIrregular)
                                {
                                    int leftover = numerator - count;
                                    if (markersPerClick < leftover && 2 * leftover <= triplSpacing)
                                    {
                                        clickSpacing = leftover;
                                    }
                                }
                            }
                        }
                        currSig->Key += ticksPerMeasure;
                    }

                    if (currSig + 1 == end)
                    {
                        buffer.Ticks = endTime;
                        buffer.Seconds = sync.ConvertToSeconds(endTime, ref tempoIndex);
                        beats.Append(buffer, BeatlineType.Measure);
                    }
                }
            }
        }
    }
}
