using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static BasicInstrumentTrack2<DrumNote2<TPads>> ConvertToBasic<TPads>(this BasicInstrumentTrack2<ProDrumNote2<FiveLane>> source)
            where TPads : unmanaged, IDrumPadConfig<TPads>
        {
            var newTrack = new BasicInstrumentTrack2<DrumNote2<TPads>>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertToBasic(source, newTrack);
        }

        public static BasicInstrumentTrack2<DrumNote2<TPads>> ConvertToBasic<TPads>(this BasicInstrumentTrack2<ProDrumNote2<FiveLane>> source, BasicInstrumentTrack2<DrumNote2<TPads>> destination)
            where TPads : unmanaged, IDrumPadConfig<TPads>
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = ConvertToBasic<TPads>(diff);
                    source[i] = null;
                }
            }
            return destination;
        }

        private static DifficultyTrack2<DrumNote2<TPads>> ConvertToBasic<TPads>(DifficultyTrack2<ProDrumNote2<FiveLane>> source)
            where TPads : unmanaged, IDrumPadConfig<TPads>
        {
            var newDifficulty = new DifficultyTrack2<DrumNote2<TPads>>()
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };

            newDifficulty.Notes.Capacity = source.Notes.Count;
            unsafe
            {
                var end = source.Notes.End;
                for (var curr = source.Notes.Data; curr < end; ++curr)
                {
                    newDifficulty.Notes.Append(curr->Key, in *(DrumNote2<TPads>*) &curr->Value);
                }
            }
            source.Notes.Dispose();
            return newDifficulty;
        }

        public static BasicInstrumentTrack2<ProDrumNote2<FourLane>> ConvertToPro(this BasicInstrumentTrack2<ProDrumNote2<FiveLane>> source)
        {
            var newTrack = new BasicInstrumentTrack2<ProDrumNote2<FourLane>>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertToPro(source, newTrack);
        }

        public static BasicInstrumentTrack2<ProDrumNote2<FourLane>> ConvertToPro(this BasicInstrumentTrack2<ProDrumNote2<FiveLane>> source, BasicInstrumentTrack2<ProDrumNote2<FourLane>> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = ConvertToPro(diff);
                    source[i] = null;
                }
            }
            return destination;
        }

        private static DifficultyTrack2<ProDrumNote2<FourLane>> ConvertToPro(DifficultyTrack2<ProDrumNote2<FiveLane>> source)
        {
            var newDifficulty = new DifficultyTrack2<ProDrumNote2<FourLane>>()
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };

            newDifficulty.Notes.Capacity = source.Notes.Count;
            unsafe
            {
                ProDrumNote2<FourLane> buffer = default;
                var end = source.Notes.End;
                for (var curr = source.Notes.Data; curr < end; ++curr)
                {
                    Buffer.MemoryCopy(&curr->Value, &buffer, sizeof(ProDrumNote2<FourLane>), sizeof(DrumNote2<FourLane>));
                    buffer.Cymbals.Yellow = curr->Value.Cymbals.Yellow;
                    buffer.Cymbals.Blue   = curr->Value.Cymbals.Blue;
                    buffer.Cymbals.Green  = curr->Value.Cymbals.Green;
                    newDifficulty.Notes.Append(curr->Key, in buffer);
                }
            }
            source.Notes.Dispose();
            return newDifficulty;
        }
    }
}
