using YARG.Core.Containers;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    internal static class CommonTrackOps
    {
        internal static YargNativeList<HittablePhrase> InitHittablePhrases(YargNativeSortedList<DualTime, DualTime> phrases)
        {
            var hittablePhrases = new YargNativeList<HittablePhrase>()
            {
                Capacity = phrases.Count
            };
            hittablePhrases.Resize_NoInitialization(phrases.Count);

            for (int i = 0; i < phrases.Count; i++)
            {
                ref readonly var phrase = ref phrases[i];
                hittablePhrases[i] = new HittablePhrase(phrase.Key, phrase.Key + phrase.Value);
            }

            return hittablePhrases;
        }

        internal static int GetHittablePhraseIndex(
            YargNativeList<HittablePhrase> phrases,
            DualTime position,
            ref int phraseIndex
        )
        {
            while (phraseIndex < phrases.Count && phrases[phraseIndex].EndTime <= position)
            {
                phraseIndex++;
            }

            if (phraseIndex >= phrases.Count || position < phrases[phraseIndex].StartTime)
            {
                return -1;
            }
            phrases[phraseIndex].TotalNotes++;
            return phraseIndex;
        }

        internal static int GetBestPositionIndex(this YargNativeList<DualTime> list, long ticks, int lo = 0, int hi = int.MaxValue)
        {
            int index = list.Find(ticks, lo, hi);
            if (index < 0)
            {
                index = ~index;
            }
            return index;
        }

        internal static int GetBestPositionIndex(this YargNativeList<DualTime> list, double seconds, int lo = 0, int hi = int.MaxValue)
        {
            int index = list.Find(seconds, lo, hi);
            if (index < 0)
            {
                index = ~index;
            }
            return index;
        }
    }
}
