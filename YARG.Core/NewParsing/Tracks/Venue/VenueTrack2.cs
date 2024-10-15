using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class VenueTrack2 : ITrack
    {
        public YARGNativeSortedList<DualTime, VenueEvent2<LightingType>> Lighting = YARGNativeSortedList<DualTime, VenueEvent2<LightingType>>.Default;
        public YARGNativeSortedList<DualTime, VenueEvent2<PostProcessingType>> PostProcessing = YARGNativeSortedList<DualTime, VenueEvent2<PostProcessingType>>.Default;
        public YARGNativeSortedList<DualTime, VenueEvent2<PerformerEvent2>> Performer = YARGNativeSortedList<DualTime, VenueEvent2<PerformerEvent2>>.Default;
        public YARGNativeSortedList<DualTime, VenueEvent2<StageEffect>> Stage = YARGNativeSortedList<DualTime, VenueEvent2<StageEffect>>.Default;

        public long NativeMemoryUsage =>
            Lighting.MemoryUsage
            + PostProcessing.MemoryUsage
            + Performer.MemoryUsage
            + Stage.MemoryUsage;
            
        public bool IsEmpty()
        {
            return Lighting.IsEmpty()
                && PostProcessing.IsEmpty()
                && Performer.IsEmpty()
                && Stage.IsEmpty();
        }

        public void TrimExcess()
        {
            Lighting.TrimExcess();
            PostProcessing.TrimExcess();
            Performer.TrimExcess();
            Stage.TrimExcess();
        }

        public void Clear()
        {
            Lighting.Clear();
            PostProcessing.Clear();
            Performer.Clear();
            Stage.Clear();
        }

        public void Dispose()
        {
            Lighting.Dispose();
            PostProcessing.Dispose();
            Performer.Dispose();
            Stage.Dispose();
        }
    }
}
