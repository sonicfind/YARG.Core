using System;
using YARG.Core.Containers;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    internal static class CommonTrackOps
    {
        internal static FixedArray<HittablePhrase> InitHittablePhrases(YargNativeSortedList<DualTime, DualTime> phrases)
        {
            var hittablePhrases = FixedArray<HittablePhrase>.Alloc(phrases.Count);

            for (int i = 0; i < phrases.Count; i++)
            {
                ref readonly var phrase = ref phrases[i];
                hittablePhrases[i] = new HittablePhrase(phrase.Key, phrase.Key + phrase.Value);
            }

            return hittablePhrases;
        }

        internal static int GetHittablePhraseIndex(
            FixedArray<HittablePhrase> phrases,
            DualTime position,
            ref int phraseIndex
        )
        {
            while (phraseIndex < phrases.Length && phrases[phraseIndex].EndTime <= position)
            {
                phraseIndex++;
            }

            if (phraseIndex >= phrases.Length || position < phrases[phraseIndex].StartTime)
            {
                return -1;
            }
            phrases[phraseIndex].TotalNotes++;
            return phraseIndex;
        }

        internal static int GetBestPositionIndex<T, U>(this FixedArray<T> list, U value, int lo = 0, int hi = int.MaxValue)
            where T : unmanaged, IComparable<U>
            where U : unmanaged
        {
            if (lo < 0)
            {
                lo = 0;
            }

            if (hi > list.Length)
            {
                hi = list.Length;
            }
            hi--;

            while (lo <= hi)
            {
                int curr = (hi + lo) >> 1;
                int order = list[curr].CompareTo(value);
                if (order == 0)
                {
                    return curr;
                }

                if (order < 0)
                {
                    lo = curr + 1;
                }
                else
                {
                    hi = curr - 1;
                }
            }
            return lo;
        }
    }
}
