using YARG.Core.Chart;

namespace YARG.Core.Parsing
{
    public static class YARGChartFinalizer
    {
        public static void FinalizeTempoMap(SyncTrack_FW sync)
        {
            var tempos = sync.TempoMarkers;
            var sigs = sync.TimeSigs;
            if (tempos.IsEmpty() || tempos.At_index(0).position != 0)
                tempos.Insert(0, 0, Tempo_FW.DEFAULT);

            if (sigs.IsEmpty() || sigs.At_index(0).position != 0)
                sigs.Insert(0, 0, TimeSig_FW.DEFAULT);

            unsafe
            {
                var prevNode = tempos.Data;
                var end = tempos.Data + tempos.Count;
                uint tickrate = sync.Tickrate;

                // We can skip the first Anchor, even if not explicitly set (as it'd still be 0)
                for (var marker = prevNode + 1; marker < end; marker++, prevNode++)
                {
                    if (marker->obj.Anchor == 0)
                    {
                        marker->obj.Anchor = (long) (((marker->position - prevNode->position) / (float) tickrate) * prevNode->obj.Micros) + prevNode->obj.Anchor;
                    }
                }
            }
        }

        public static DualTime GetEndTime(YARGChart chart)
        {
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

            var lastNoteTime = DualTime.Zero;
            foreach (var track in tracks)
            {
                if (track != null)
                {
                    var lastTime = track.GetLastNoteTime();
                    if (lastTime > lastNoteTime)
                        lastNoteTime = lastTime;
                }
            }

            var endTime = lastNoteTime;
            var globals = chart.Events.globals.Span;
            for (int i = globals.Length - 1; i >= 0; --i)
            {
                foreach (string ev in globals[i].obj)
                {
                    if (ev == "[end]")
                    {
                        if (lastNoteTime < globals[i].position)
                            endTime = globals[i].position;
                        return endTime;
                    }
                }
            }

            if (globals.Length > 0)
            {
                var node = globals[^1];
                if (lastNoteTime < node.position)
                    endTime = node.position;
            }
            return endTime;
        }

        public static void FinalizeBeats(SyncTrack_FW sync)
        {
            if (sync.BeatMap.IsEmpty())
            {
                GenerateAllBeats(sync, sync.EndTime.ticks);
                sync.Style = OverdriveStyle.GuitarHero;
            }
            else
            {
                GenerateLeftoverBeats(sync, sync.EndTime.ticks);
                sync.Style = OverdriveStyle.RockBand;
            }
        }

        private static void GenerateLeftoverBeats(SyncTrack_FW sync, long endTick)
        {
            uint multipliedTickrate = 4u * sync.Tickrate;
            int searchIndex = 0;
            int tempoIndex = 0;

            var beats = sync.BeatMap;
            unsafe
            {
                var sigs = sync.TimeSigs.Data;
                int numSigs = sync.TimeSigs.Count;

                for (int i = 0; i < numSigs; ++i)
                {
                    ref var node = ref sigs[i];

                    long ticksPerMarker = multipliedTickrate >> node.obj.Denominator;
                    long ticksPerMeasure = (multipliedTickrate * node.obj.Numerator) >> node.obj.Denominator;

                    long endTime = i + 1 < numSigs ? sigs[i + 1].position : endTick;
                    while (node.position < endTime)
                    {
                        long position = node.position;
                        for (uint n = 0; n < node.obj.Numerator && position < endTime; ++n, position += ticksPerMarker, ++searchIndex)
                        {
                            var beat = new DualTime(position, sync.ConvertPositionToSeconds(position, ref tempoIndex));
                            if (!beats.Contains(searchIndex, beat))
                                beats[beat] = BeatlineType.Weak;
                        }
                        node.position += ticksPerMeasure;
                    }
                }
            }    
        }

        private static void GenerateAllBeats(SyncTrack_FW sync, long endTick)
        {
            uint multipliedTickrate = 4u * sync.Tickrate;
            int tempoIndex = 0;

            var beats = sync.BeatMap;
            unsafe
            {
                var sigs = sync.TimeSigs.Data;
                int numSigs = sync.TimeSigs.Count;

                for (int i = 0; i < numSigs; ++i)
                {
                    ref var node = ref sigs[i];
                    int numerator = node.obj.Numerator;

                    int markersPerClick = (6 << node.obj.Denominator) / node.obj.Metronome;
                    long ticksPerMarker = multipliedTickrate >> node.obj.Denominator;
                    long ticksPerMeasure = (multipliedTickrate * numerator) >> node.obj.Denominator;
                    bool isIrregular = numerator > 4 || (numerator & 1) == 1;

                    long endTime = i + 1 < numSigs ? sigs[i + 1].position : endTick;
                    while (node.position < endTime)
                    {
                        long position = node.position;
                        var style = BeatlineType.Measure;
                        int clickSpacing = markersPerClick;
                        int triplSpacing = 3 * markersPerClick;
                        for (int leftover = numerator; leftover > 0 && (position < endTime || i + 1 == numSigs);)
                        {
                            int clicksLeft = clickSpacing;
                            do
                            {
                                var beat = new DualTime(position, sync.ConvertPositionToSeconds(position, ref tempoIndex));
                                beats.Add_NoReturn(beat, style);
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
                        node.position += ticksPerMeasure;
                    }
                }
            }
        }
    }
}
