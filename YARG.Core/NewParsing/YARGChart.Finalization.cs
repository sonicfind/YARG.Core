using System;
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
        private static unsafe void FinalizeSyncTrack(SyncTrack2 sync, double resolution)
        {
            // We can skip the first Anchor, even if not explicitly set (as it'd still be 0)
            for (int index = 1; index < sync.TempoMarkers.Count; ++index)
            {
                ref var currMarker = ref sync.TempoMarkers[index];
                ref var prevMarker = ref sync.TempoMarkers[index - 1];
                double numQuarters = (currMarker.Key - prevMarker.Key) / resolution;
                if (currMarker.Value.PositionInMicroseconds == 0)
                {
                    currMarker.Value.PositionInMicroseconds = (long) (numQuarters * prevMarker.Value.MicrosecondsPerQuarter) + prevMarker.Value.PositionInMicroseconds;
                }
                else
                {
                    prevMarker.Value.MicrosecondsPerQuarter = (long) ((currMarker.Value.PositionInMicroseconds - prevMarker.Value.PositionInMicroseconds) / numQuarters);
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
        private static void GenerateLeftoverBeats(YARGChart chart, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * chart.Resolution;

            int beatIndex = 0;
            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);

            var buffer = DualTime.Zero;
            for (int index = 0; index < chart.Sync.TimeSigs.Count; ++index)
            {
                ref readonly var timeSig = ref chart.Sync.TimeSigs[index];
                long ticksPerMarker = multipliedTickrate >> timeSig.Value.Denominator;
                long ticksPerMeasure = (multipliedTickrate * timeSig.Value.Numerator) >> timeSig.Value.Denominator;

                long endTime;
                if (index + 1 < chart.Sync.TimeSigs.Count)
                {
                    endTime = chart.Sync.TimeSigs[index + 1].Key;
                }
                else
                {
                    endTime = endPosition.Ticks;
                    long tickDisplacement = endTime - timeSig.Key;
                    long mod = tickDisplacement % ticksPerMeasure;
                    if (mod > 0)
                    {
                        endTime += ticksPerMeasure - mod;
                    }
                }

                long currMeasure = timeSig.Key;
                while (currMeasure < endTime)
                {
                    long currMarker = currMeasure;
                    for (uint n = 0; n < timeSig.Value.Numerator && currMarker < endTime; ++n)
                    {
                        while (beatIndex < chart.BeatMap.Count && chart.BeatMap[beatIndex].Key.Ticks < currMarker)
                        {
                            ++beatIndex;
                        }

                        if (beatIndex == chart.BeatMap.Count || currMarker < chart.BeatMap[beatIndex].Key.Ticks)
                        {
                            buffer.Ticks = currMarker;
                            buffer.Seconds = tempoTracker.Convert(currMarker);
                            chart.BeatMap.Insert(beatIndex, (buffer, BeatlineType.Weak));
                        }
                        ++beatIndex;
                        currMarker += ticksPerMarker;
                    }
                    currMeasure += ticksPerMeasure;
                }

                if (index + 1 == chart.Sync.TimeSigs.Count)
                {
                    buffer.Ticks = endTime;
                    buffer.Seconds = tempoTracker.Convert(endTime);
                    chart.BeatMap.Add(buffer, BeatlineType.Measure);
                }
            }
        }

        /// <summary>
        /// Generates the entire beat track for the provided chart.
        /// </summary>
        /// <param name="chart">The chart with the beattrack to alter</param>
        /// <param name="endPosition">The position where the chart data should stop</param>
        private static void GenerateAllBeats(YARGChart chart, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * chart.Resolution;
            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);

            var buffer = DualTime.Zero;
            for (int index = 0; index < chart.Sync.TimeSigs.Count; ++index)
            {
                ref readonly var timeSig = ref chart.Sync.TimeSigs[index];
                int numerator = timeSig.Value.Numerator;
                int markersPerClick = (6 << timeSig.Value.Denominator) / timeSig.Value.Metronome;
                long ticksPerMarker = multipliedTickrate >> timeSig.Value.Denominator;
                long ticksPerMeasure = (multipliedTickrate * numerator) >> timeSig.Value.Denominator;
                bool isIrregular = (numerator & 1) == 1 && (numerator % 3) > 0;

                long endTime;
                if (index + 1 < chart.Sync.TimeSigs.Count)
                {
                    endTime = chart.Sync.TimeSigs[index + 1].Key;
                }
                else
                {
                    endTime = endPosition.Ticks;
                    long tickDisplacement = endTime - timeSig.Key;
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

                long currMeasure = timeSig.Key;
                while (currMeasure < endTime)
                {
                    long currMarker = currMeasure;
                    for (int i = 0; i < numerator && currMarker < endTime; ++i, currMarker += ticksPerMarker)
                    {
                        buffer.Ticks = currMarker;
                        buffer.Seconds = tempoTracker.Convert(currMarker);
                        chart.BeatMap.Add(buffer, pattern[i]);
                    }
                    currMeasure += ticksPerMeasure;
                }

                if (index + 1 == chart.Sync.TimeSigs.Count)
                {
                    buffer.Ticks = endTime;
                    buffer.Seconds = tempoTracker.Convert(endTime);
                    chart.BeatMap.Add(buffer, BeatlineType.Measure);
                }
            }
        }
    }
}
