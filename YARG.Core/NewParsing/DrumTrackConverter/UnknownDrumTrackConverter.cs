using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static BasicInstrumentTrack2<TDrumNote> ConvertTo<TDrumNote, TPads>(BasicInstrumentTrack2<ProDrumNote2<FiveLane>> source)
            where TDrumNote : unmanaged, IDrumNote<TPads>
            where TPads : unmanaged, IDrumPadConfig
        {
            var newTrack = new BasicInstrumentTrack2<TDrumNote>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertTo<TDrumNote, TPads>(newTrack, source);
        }

        public static BasicInstrumentTrack2<TDrumNote> ConvertTo<TDrumNote, TPads>(BasicInstrumentTrack2<TDrumNote> destination, BasicInstrumentTrack2<ProDrumNote2<FiveLane>> source)
            where TDrumNote : unmanaged, IDrumNote<TPads>
            where TPads : unmanaged, IDrumPadConfig
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = ConvertTo<TDrumNote, TPads>(diff);
                    source[i] = null;
                }
            }
            return destination;
        }

        private static DifficultyTrack2<TDrumNote> ConvertTo<TDrumNote, TPads>(DifficultyTrack2<ProDrumNote2<FiveLane>> source)
            where TDrumNote : unmanaged, IDrumNote<TPads>
            where TPads : unmanaged, IDrumPadConfig
        {
            var newDifficulty = new DifficultyTrack2<TDrumNote>()
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };

            newDifficulty.Notes.Capacity = source.Notes.Count;
            unsafe
            {
                TDrumNote buffer = default;
                var end = source.Notes.End;
                for (var curr = source.Notes.Data; curr < end; ++curr)
                {
                    buffer.LoadFrom(curr->Value);
                    // Append sends deep copies, so this is safe
                    newDifficulty.Notes.Append(curr->Key, in buffer);
                }
            }
            source.Notes.Dispose();
            return newDifficulty;
        }
    }
}
