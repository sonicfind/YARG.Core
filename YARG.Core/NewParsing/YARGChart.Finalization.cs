using System.Diagnostics;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        private static void FinalizeAnchors(SyncTrack2 sync)
        {
            Debug.Assert(sync.TempoMarkers.Count > 0, "A least one tempo marker must exist");
            unsafe
            {
                double tickrate = sync.Tickrate;
                var curr = sync.TempoMarkers.Data;
                var end = curr + sync.TempoMarkers.Count;
                while (true)
                {
                    // We can skip the first Anchor, even if not explicitly set (as it'd still be 0)
                    var prev = curr++;
                    if (curr == end)
                    {
                        break;
                    }

                    double numQuarters = (curr->Key - prev->Key) / tickrate;
                    if (curr->Value.Anchor == 0)
                    {
                        curr->Value.Anchor = (long) (numQuarters * prev->Value.MicrosPerQuarter) + prev->Value.Anchor;
                    }
                    else
                    {
                        prev->Value.MicrosPerQuarter = (int) ((curr->Value.Anchor - prev->Value.Anchor) / numQuarters);
                    }
                }
            }
        }

        private static void FinalizeDeserialization(YARGChart chart)
        {
            var endPosition = chart.GetEndTime();
            if (chart.BeatMap.IsEmpty())
            {
                GenerateAllBeats(chart, endPosition);
            }
            else
            {
                GenerateLeftoverBeats(chart, endPosition);
            }

            chart.Sync.TempoMarkers.TrimExcess();
            chart.Sync.TimeSigs.TrimExcess();
            chart.BeatMap.TrimExcess();
            chart.FiveFretGuitar?.TrimExcess();
            chart.FiveFretBass?.TrimExcess();
            chart.FiveFretRhythm?.TrimExcess();
            chart.FiveFretCoopGuitar?.TrimExcess();

            chart.SixFretGuitar?.TrimExcess();
            chart.SixFretBass?.TrimExcess();
            chart.SixFretRhythm?.TrimExcess();
            chart.SixFretCoopGuitar?.TrimExcess();

            chart.Keys?.TrimExcess();

            chart.FourLaneDrums?.TrimExcess();
            chart.FiveLaneDrums?.TrimExcess();

            // chart.TrueDrums?.TrimExcess();

            chart.ProGuitar_17Fret?.TrimExcess();
            chart.ProGuitar_22Fret?.TrimExcess();
            chart.ProBass_17Fret?.TrimExcess();
            chart.ProBass_22Fret?.TrimExcess();

            chart.ProKeys?.TrimExcess();

            // chart.DJ?.Dispose();

            chart.LeadVocals?.TrimExcess();
            chart.HarmonyVocals?.TrimExcess();
        }

        private static unsafe void GenerateLeftoverBeats(YARGChart chart, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * chart.Sync.Tickrate;

            int beatIndex = 0;
            var tempoTracker = new TempoTracker(chart.Sync);

            DualTime buffer = default;
            var end = chart.Sync.TimeSigs.End;
            for (var currSig = chart.Sync.TimeSigs.Data; currSig < end; ++currSig)
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
                    long tickDisplacement = endTime - currSig->Key;
                    long mod = tickDisplacement % ticksPerMeasure;
                    if (mod > 0)
                    {
                        endTime += ticksPerMeasure - mod;
                    }
                }

                while (currSig->Key < endTime)
                {
                    long position = currSig->Key;
                    for (uint n = 0; n < currSig->Value.Numerator && position < endTime; ++n)
                    {
                        while (beatIndex < chart.BeatMap.Count && chart.BeatMap.Data[beatIndex].Key.Ticks < position)
                        {
                            ++beatIndex;
                        }

                        if (beatIndex == chart.BeatMap.Count || position < chart.BeatMap.Data[beatIndex].Key.Ticks)
                        {
                            buffer.Ticks = position;
                            buffer.Seconds = tempoTracker.Traverse(position);
                            chart.BeatMap.Insert_Forced(beatIndex, in buffer, BeatlineType.Weak);
                        }
                        ++beatIndex;
                        position += ticksPerMarker;
                    }
                    currSig->Key += ticksPerMeasure;
                }

                if (currSig + 1 == end)
                {
                    buffer.Ticks = endTime;
                    buffer.Seconds = tempoTracker.Traverse(endTime);
                    chart.BeatMap.Append(buffer, BeatlineType.Measure);
                }
            }
        }

        private static unsafe void GenerateAllBeats(YARGChart chart, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * chart.Sync.Tickrate;
            var tempoTracker = new TempoTracker(chart.Sync);

            DualTime buffer = default;
            var end = chart.Sync.TimeSigs.End;
            for (var currSig = chart.Sync.TimeSigs.Data; currSig < end; ++currSig)
            {
                int numerator = currSig->Value.Numerator;
                int markersPerClick = (6 << currSig->Value.Denominator) / currSig->Value.Metronome;
                long ticksPerMarker = multipliedTickrate >> currSig->Value.Denominator;
                long ticksPerMeasure = (multipliedTickrate * numerator) >> currSig->Value.Denominator;
                bool isIrregular = (numerator & 1) == 1 && (numerator % 3) > 0;

                long endTime;
                if (currSig + 1 < end)
                {
                    endTime = currSig[1].Key;
                }
                else
                {
                    endTime = endPosition.Ticks;
                    long tickDisplacement = endTime - currSig->Key;
                    long mod = tickDisplacement % ticksPerMeasure;
                    if (mod > 0)
                    {
                        endTime += ticksPerMeasure - mod;
                    }
                }

                var pattern = new BeatlineType[numerator];
                // 0 = measure
                for (int i = 1; i < pattern.Length; ++i)
                {
                    if (markersPerClick > 1 && (i % markersPerClick) > 0)
                    {
                        pattern[i] = BeatlineType.Weak;
                    }
                    else
                    {
                        pattern[i] = BeatlineType.Strong;
                        if (isIrregular)
                        {
                            int leftover = numerator - i;
                            if (markersPerClick < leftover && 2 * leftover <= 3 * markersPerClick)
                            {
                                markersPerClick = leftover;
                            }
                        }
                    }
                }

                while (currSig->Key < endTime)
                {
                    long position = currSig->Key;
                    for (int i = 0; i < numerator && position < endTime; ++i, position += ticksPerMarker)
                    {
                        buffer.Ticks = position;
                        buffer.Seconds = tempoTracker.Traverse(position);
                        chart.BeatMap.Append(buffer, pattern[i]);
                    }
                    currSig->Key += ticksPerMeasure;
                }

                if (currSig + 1 == end)
                {
                    buffer.Ticks = endTime;
                    buffer.Seconds = tempoTracker.Traverse(endTime);
                    chart.BeatMap.Append(buffer, BeatlineType.Measure);
                }
            }
        }
    }
}
