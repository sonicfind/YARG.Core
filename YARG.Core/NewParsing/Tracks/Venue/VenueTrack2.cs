using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class VenueTrack2 : IDisposable
    {
        public YARGNativeSortedList<DualTime, VenueEvent2<LightingType>> Lighting = YARGNativeSortedList<DualTime, VenueEvent2<LightingType>>.Default;
        public YARGNativeSortedList<DualTime, VenueEvent2<PostProcessingType>> PostProcessing = YARGNativeSortedList<DualTime, VenueEvent2<PostProcessingType>>.Default;
        public YARGNativeSortedList<DualTime, VenueEvent2<PerformerEvent2>> Performer = YARGNativeSortedList<DualTime, VenueEvent2<PerformerEvent2>>.Default;
        public YARGNativeSortedList<DualTime, VenueEvent2<StageEffect>> Stage = YARGNativeSortedList<DualTime, VenueEvent2<StageEffect>>.Default;

        public void Dispose()
        {
            Lighting.Dispose();
            PostProcessing.Dispose();
            Performer.Dispose();
            Stage.Dispose();
        }
    }
}
