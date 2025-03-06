using YARG.Core.Containers;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public static class CommonTrackCacheOps
    {
        public static long GetOverdrivePhraseIndex(
            YargNativeSortedList<DualTime, DualTime> trackOverdrives,
            YargNativeList<OverdrivePhrase> cacheOverdrives,
            in DualTime position,
            ref long phraseIndex
        )
        {
            // This value will only ever change if the position lies within a phrase
            long resultIndex = -1;
            while (phraseIndex < trackOverdrives.Count)
            {
                ref readonly var overdrive = ref trackOverdrives[phraseIndex];
                var phraseEndTime = overdrive.Key + overdrive.Value;
                if (position < phraseEndTime)
                {
                    if (position >= overdrive.Key)
                    {
                        cacheOverdrives[phraseIndex].TotalNotes++;
                        resultIndex = phraseIndex;
                    }
                    break;
                }
                phraseIndex++;
            }
            return resultIndex;
        }

        public static long GetSoloPhraseIndex(
            YargNativeList<SoloPhrase> cacheSolos,
            in DualTime position,
            ref long phraseIndex
        )
        {
            // This value will only ever change if the position lies within a phrase
            long resultIndex = -1;
            while (phraseIndex < cacheSolos.Count)
            {
                ref var solo = ref cacheSolos[phraseIndex];
                if (position < solo.EndTime)
                {
                    if (position >= solo.StartTime)
                    {
                        solo.TotalNotes++;
                        resultIndex = phraseIndex;
                    }
                    break;
                }
                phraseIndex++;
            }
            return resultIndex;
        }
    }
}