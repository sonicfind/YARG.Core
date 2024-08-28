using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class VenueTrack2 : IDisposable
    {
        public readonly YARGNativeSortedList<DualTime, VenueEvent2<LightingType>> Lighting = new();
        public readonly YARGNativeSortedList<DualTime, VenueEvent2<PostProcessingType>> PostProcessing = new();
        public readonly YARGNativeSortedList<DualTime, VenueEvent2<PerformerEvent2>> Performer = new();
        public readonly YARGNativeSortedList<DualTime, VenueEvent2<StageEffect>> Stage = new();

        public void Dispose()
        {
            Lighting.Dispose();
            PostProcessing.Dispose();
            Performer.Dispose();
            Stage.Dispose();
        }
    }
}
