using YARG.Core.Containers;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct Sustain
    {
        public readonly DualTime EndTime;
        public          int      NoteMask;

        public Sustain(in DualTime endTime)
        {
            EndTime = endTime;
            NoteMask = 0;
        }
    }

    public static class CommonTrackCacheOps
    {
        public static long GetPhraseIndex(
            YargNativeSortedList<DualTime, DualTime> trackPhrases,
            YargNativeSortedList<DualTime, HittablePhrase> cachePhrases,
            in DualTime position,
            ref long phraseIndex
        )
        {
            // This value will only ever change if the position lies within a phrase
            long resultIndex = -1;
            while (phraseIndex < trackPhrases.Count)
            {
                ref readonly var overdrive = ref trackPhrases[phraseIndex];
                var phraseEndTime = overdrive.Key + overdrive.Value;
                if (position < phraseEndTime)
                {
                    if (position >= overdrive.Key)
                    {
                        unsafe
                        {
                            if (cachePhrases.GetLastOrAdd(overdrive.Key, out var phrase))
                            {
                                // No reason to set the end time more than once, am I right?
                                phrase->EndTime = phraseEndTime;
                            }
                            phrase->TotalNotes++;
                        }
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