using YARG.Core.Chart;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class VenueTrack2 : ITrack
    {
        public YARGNativeSortedList<DualTime, VenueEvent2<LightingType>> Lighting { get; } = new();
        public YARGNativeSortedList<DualTime, VenueEvent2<PostProcessingType>> PostProcessing { get; } = new();
        public YARGNativeSortedList<DualTime, VenueEvent2<PerformerEvent2>> Performer { get; } = new();
        public YARGNativeSortedList<DualTime, VenueEvent2<StageEffect>> Stage { get; } = new();

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
