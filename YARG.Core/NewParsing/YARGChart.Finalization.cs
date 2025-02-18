using System.Diagnostics;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        /// <summary>
        /// Traverses through every tempo marker in the provided sync to set all anchors to their
        /// appropriate microseconds positions.
        /// </summary>
        /// <remarks>
        /// If an anchor is already set, it will instead, for consistency, alter the tempomarker that comes before
        /// to ensure as correct alignment as possible.
        /// <br></br><br></br>
        /// This MUST be called following the deserialization of a sync track from either file format.
        /// </remarks>
        /// <param name="sync">The synctrack to finalize</param>
        private static unsafe void FinalizeAnchors(SyncTrack2 sync, double resolution)
        {
            Debug.Assert(sync.TempoMarkers.Count > 0, "A least one tempo marker must exist");
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

                double numQuarters = (curr->Key - prev->Key) / resolution;
                if (curr->Value.PositionInMicroseconds == 0)
                {
                    curr->Value.PositionInMicroseconds = (long) (numQuarters * prev->Value.MicrosecondsPerQuarter) + prev->Value.PositionInMicroseconds;
                }
                else
                {
                    prev->Value.MicrosecondsPerQuarter = (int) ((curr->Value.PositionInMicroseconds - prev->Value.PositionInMicroseconds) / numQuarters);
                }
            }
        }

        /// <summary>
        /// Generates or fills in the chart's beat track and trims any excess data leftover from track deserialization
        /// </summary>
        /// <param name="chart"></param>
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
            chart.Venue.TrimExcess();

            chart._fiveFretGuitar?.TrimExcess();
            chart._fiveFretBass?.TrimExcess();
            chart._fiveFretRhythm?.TrimExcess();
            chart._fiveFretCoopGuitar?.TrimExcess();
            chart._keys?.TrimExcess();

            chart._sixFretGuitar?.TrimExcess();
            chart._sixFretBass?.TrimExcess();
            chart._sixFretRhythm?.TrimExcess();
            chart._sixFretCoopGuitar?.TrimExcess();

            chart._fourLaneDrums?.TrimExcess();
            chart._fiveLaneDrums?.TrimExcess();

            // chart._eliteDrums?.TrimExcess();

            chart._proGuitar_17Fret?.TrimExcess();
            chart._proGuitar_22Fret?.TrimExcess();
            chart._proBass_17Fret?.TrimExcess();
            chart._proBass_22Fret?.TrimExcess();

            chart._proKeys?.TrimExcess();

            chart._leadVocals?.TrimExcess();
            chart._harmonyVocals?.TrimExcess();
        }

        /// <summary>
        /// Traverses through a deserialized beattrack to fill empty gaps with weak beats.
        /// </summary>
        /// <param name="chart">The chart with the beattrack to alter</param>
        /// <param name="endPosition">The position where the chart data should stop</param>
        private static unsafe void GenerateLeftoverBeats(YARGChart chart, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * chart.Resolution;

            int beatIndex = 0;
            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);

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
                            chart.BeatMap.Insert(beatIndex, (buffer, BeatlineType.Weak));
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
                    chart.BeatMap.Add(buffer, BeatlineType.Measure);
                }
            }
        }

        /// <summary>
        /// Generates the entire beat track for the provided chart.
        /// </summary>
        /// <param name="chart">The chart with the beattrack to alter</param>
        /// <param name="endPosition">The position where the chart data should stop</param>
        private static unsafe void GenerateAllBeats(YARGChart chart, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * chart.Resolution;
            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);

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
                        chart.BeatMap.Add(buffer, pattern[i]);
                    }
                    currSig->Key += ticksPerMeasure;
                }

                if (currSig + 1 == end)
                {
                    buffer.Ticks = endTime;
                    buffer.Seconds = tempoTracker.Traverse(endTime);
                    chart.BeatMap.Add(buffer, BeatlineType.Measure);
                }
            }
        }
    }
}
