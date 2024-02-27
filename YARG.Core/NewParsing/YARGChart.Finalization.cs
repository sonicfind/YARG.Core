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

        public static DualTime GetEndTime(YARGChart chart)
        {
            var globals = chart.Events.Globals.Span;
            for (int i = globals.Length - 1; i >= 0; --i)
            {
                foreach (string ev in globals[i].Value)
                {
                    if (ev == "[end]")
                    {
                        return globals[i].Key;
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
            return lastNoteTime;
        }

        public static void FinalizeBeats(YARGChart chart)
        {
            var endTime = GetEndTime(chart);
            if (chart.BeatMap.IsEmpty())
            {
                GenerateAllBeats(chart.Sync, endTime.Ticks, chart.BeatMap);
            }
            else
            {
                GenerateLeftoverBeats(chart.Sync, endTime.Ticks, chart.BeatMap);
            }
        }

        public static void SetSustainThreshold(LoaderSettings settings, uint defaultThreshold)
        {
            DualTime.TruncationLimit = settings.SustainCutoffThreshold != -1 ? settings.SustainCutoffThreshold : defaultThreshold;
        }

        private static void GenerateLeftoverBeats(SyncTrack2 sync, long endTick, YARGNativeSortedList<DualTime, BeatlineType> beats)
        {
            uint multipliedTickrate = 4u * sync.Tickrate;

            int searchIndex = 0;
            int tempoIndex = 0;

            DualTime beat = default;
            unsafe
            {
                var currSig = sync.TimeSigs.Data;
                var end = sync.TimeSigs.End;
                while (currSig < end)
                {
                    long ticksPerMarker = multipliedTickrate >> currSig->Value.Denominator;
                    long ticksPerMeasure = (multipliedTickrate * currSig->Value.Numerator) >> currSig->Value.Denominator;

                    long endTime = currSig + 1 < end ? currSig[1].Key : endTick;
                    while (currSig->Key < endTime)
                    {
                        long position = currSig->Key;
                        for (uint n = 0; n < currSig->Value.Numerator && position < endTime; ++n, position += ticksPerMarker, ++searchIndex)
                        {
                            beat.Ticks = position;
                            beat.Seconds = sync.ConvertToSeconds(position, ref tempoIndex);
                            if (!beats.ContainsKey(searchIndex, beat))
                            {
                                beats[beat] = BeatlineType.Weak;
                            }
                        }
                        currSig->Key += ticksPerMeasure;
                    }

                }
            }
        }

        private static void GenerateAllBeats(SyncTrack2 sync, long endTick, YARGNativeSortedList<DualTime, BeatlineType> beats)
        {
            uint multipliedTickrate = 4u * sync.Tickrate;

            int tempoIndex = 0;
            DualTime beat = default;
            unsafe
            {
                var currSig = sync.TimeSigs.Data;
                var end = sync.TimeSigs.End;
                while (currSig < end)
                {
                    int numerator = currSig->Value.Numerator;
                    int markersPerClick = (6 << currSig->Value.Denominator) / currSig->Value.Metronome;
                    long ticksPerMarker = multipliedTickrate >> currSig->Value.Denominator;
                    long ticksPerMeasure = (multipliedTickrate * numerator) >> currSig->Value.Denominator;
                    bool isIrregular = numerator > 4 || (numerator & 1) == 1;

                    long endTime = currSig + 1 < end ? currSig[1].Key : endTick;
                    while (currSig->Key < endTime)
                    {
                        var style = BeatlineType.Measure;
                        long position = currSig->Key;
                        int clickSpacing = markersPerClick;
                        int triplSpacing = 3 * markersPerClick;
                        for (int leftover = numerator; leftover > 0 && (position < endTime || currSig + 1 == end);)
                        {
                            int clicksLeft = clickSpacing;
                            do
                            {
                                beat.Ticks = position;
                                beat.Seconds = sync.ConvertToSeconds(position, ref tempoIndex);
                                beats.Append(beat, style);

                                position += ticksPerMarker;
                                style = BeatlineType.Weak;
                                --clicksLeft;
                                --leftover;
                            } while (clicksLeft > 0 && leftover > 0 && position < endTime);

                            style = BeatlineType.Strong;

                            if (isIrregular && leftover > 0 && position < endTime && markersPerClick < leftover && 2 * leftover <= triplSpacing)
                            {
                                // leftover < 1.5 * spacing
                                clickSpacing = leftover;
                            }
                        }
                        currSig->Key += ticksPerMeasure;
                    }
                }
            }
        }
    }
}
