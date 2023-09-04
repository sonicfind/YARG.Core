namespace YARG.Core.Chart
{
    public static class SyncTrackFinalizer
    {
        public static void FinalizeTempoMap(SyncTrack_FW sync)
        {
            var tempos = sync.TempoMarkers;
            var sigs = sync.TimeSigs;
            if (tempos.IsEmpty() || tempos.At_index(0).position != 0)
                tempos.Insert(0, 0, new(Tempo_FW.MICROS_AT_120BPM));

            if (sigs.IsEmpty() || sigs.At_index(0).position != 0)
                sigs.Insert(0, 0, new(4, 2, 24, 8));
            else
            {
                ref var timeSig = ref sigs.At_index(0).obj;
                if (timeSig.Denominator == 255)
                    timeSig.Denominator = 2;
            }

            unsafe
            {
                var prevNode = tempos.Data;
                var end = tempos.Data + tempos.Count;
                uint tickrate = sync.Tickrate;
                for (var marker = prevNode + 1; marker < end; marker++, prevNode++)
                    if (marker->obj.Anchor == 0)
                        marker->obj.Anchor = (long) (((marker->position - prevNode->position) / (float) tickrate) * prevNode->obj.Micros) + prevNode->obj.Anchor;
            }
        }

        public static void FinalizeBeats(SyncTrack_FW sync, long endTick)
        {
            if (sync.BeatMap.IsEmpty())
                GenerateAllBeats(sync, endTick);
            else
                GenerateLeftoverBeats(sync, endTick);
        }

        private static void GenerateLeftoverBeats(SyncTrack_FW sync, long endTick)
        {
            uint multipliedTickrate = 4u * sync.Tickrate;
            uint denominator = 0;
            int searchIndex = 0;
            int tempoIndex = 0;

            var beats = sync.BeatMap;
            var sigs = sync.TimeSigs.Span;
            int numSigs = sigs.Length;
            for (int i = 0; i < numSigs; ++i)
            {
                var node = sigs[i];
                if (node.obj.Denominator != 255)
                    denominator = 1u << node.obj.Denominator;

                long ticksPerMarker = multipliedTickrate / denominator;
                long ticksPerMeasure = (multipliedTickrate * node.obj.Numerator) / denominator;
                long endTime;
                if (i + 1 < numSigs)
                    endTime = sigs[i + 1].position;
                else
                    endTime = endTick;

                while (node.position < endTime)
                {
                    long position = node.position;
                    for (uint n = 0; n < node.obj.Numerator && position < endTime; ++n, position += ticksPerMarker, ++searchIndex)
                    {
                        var beat = new DualPosition(position, sync.ConvertToSeconds(position, ref tempoIndex));
                        if (!beats.Contains(searchIndex, beat))
                            beats[beat] = BeatlineType.Weak;
                    }
                    node.position += ticksPerMeasure;
                }
            }
        }

        private static void GenerateAllBeats(SyncTrack_FW sync, long endTick)
        {
            uint multipliedTickrate = 4u * sync.Tickrate;
            int metronome = 24;
            int tempoIndex = 0;

            var beats = sync.BeatMap;
            var sigs = sync.TimeSigs.Span;
            int numSigs = sigs.Length;
            for (int i = 0; i < numSigs; ++i)
            {
                var node = sigs[i];
                int numerator = node.obj.Numerator > 0 ? node.obj.Numerator : 4;
                int denominator = node.obj.Denominator != 255 ? 1 << node.obj.Denominator : 4;
                if (node.obj.Metronome != 0)
                    metronome = node.obj.Metronome;

                int markersPerClick = 6 * denominator / metronome;
                long ticksPerMarker = multipliedTickrate / denominator;
                long ticksPerMeasure = (multipliedTickrate * numerator) / denominator;
                bool isIrregular = numerator > 4 || (numerator & 1) == 1;

                long endTime;
                if (i + 1 < numSigs)
                    endTime = sigs[i + 1].position;
                else
                    endTime = endTick;

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
                            var beat = new DualPosition(position, sync.ConvertToSeconds(position, ref tempoIndex));
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
