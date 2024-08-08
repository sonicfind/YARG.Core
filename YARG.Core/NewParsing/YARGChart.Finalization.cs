using YARG.Core.Chart;

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
            var globals = chart.Globals.Span;
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

            static void Test<TTrack>(TTrack? track, ref DualTime lastNoteTime)
                where TTrack : class, ITrack
            {
                if (track != null)
                {
                    var lastTime = track.GetLastNoteTime();
                    if (lastTime > lastNoteTime)
                        lastNoteTime = lastTime;
                }
            }

            DualTime lastNoteTime = default;
            Test(chart.FiveFretGuitar, ref lastNoteTime);
            Test(chart.FiveFretBass, ref lastNoteTime);
            Test(chart.FiveFretRhythm, ref lastNoteTime);
            Test(chart.FiveFretCoopGuitar, ref lastNoteTime);
            Test(chart.SixFretGuitar, ref lastNoteTime);
            Test(chart.SixFretBass, ref lastNoteTime);
            Test(chart.SixFretRhythm, ref lastNoteTime);
            Test(chart.SixFretCoopGuitar, ref lastNoteTime);

            Test(chart.Keys, ref lastNoteTime);

            Test(chart.FourLaneDrums, ref lastNoteTime);
            Test(chart.FiveLaneDrums, ref lastNoteTime);

            // Test(chart.TrueDrums, ref lastNoteTime);

            Test(chart.ProGuitar_17Fret, ref lastNoteTime);
            Test(chart.ProGuitar_22Fret, ref lastNoteTime);
            Test(chart.ProBass_17Fret, ref lastNoteTime);
            Test(chart.ProBass_22Fret, ref lastNoteTime);

            Test(chart.ProKeys, ref lastNoteTime);

            // Test(chart.Dj, ref lastNoteTime);

            Test(chart.LeadVocals, ref lastNoteTime);
            Test(chart.HarmonyVocals, ref lastNoteTime);
            return lastNoteTime;
        }

        public static void FinalizeBeats(YARGChart chart)
        {
            var endPosition = GetEndTime(chart);
            if (chart.BeatMap.IsEmpty())
            {
                GenerateAllBeats(chart.Sync, chart.BeatMap, endPosition);
            }
            else
            {
                GenerateLeftoverBeats(chart.Sync, chart.BeatMap, endPosition);
            }
        }

        private static unsafe void GenerateLeftoverBeats(SyncTrack2 sync, YARGNativeSortedList<DualTime, BeatlineType> beats, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * sync.Tickrate;

            int beatIndex = 0;
            int tempoIndex = 0;

            DualTime buffer = default;
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
                        while (beatIndex < beats.Count && beats.ElementAtIndex(beatIndex)->Key.Ticks < position)
                        {
                            ++beatIndex;
                        }

                        if (beatIndex == beats.Count || position < beats.ElementAtIndex(beatIndex)->Key.Ticks)
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

        private static unsafe void GenerateAllBeats(SyncTrack2 sync, YARGNativeSortedList<DualTime, BeatlineType> beats, in DualTime endPosition)
        {
            long multipliedTickrate = 4 * sync.Tickrate;

            int tempoIndex = 0;
            DualTime buffer = default;
            var end = sync.TimeSigs.End;
            for (var currSig = sync.TimeSigs.Data; currSig < end; ++currSig)
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
                        buffer.Seconds = sync.ConvertToSeconds(position, ref tempoIndex);
                        beats.Append(buffer, pattern[i]);
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
