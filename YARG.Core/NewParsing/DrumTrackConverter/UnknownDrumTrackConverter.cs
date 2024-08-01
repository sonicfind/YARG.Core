using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static BasicInstrumentTrack2<DrumNote2<FourLane>> ConvertToFourLane(this BasicInstrumentTrack2<DrumNote2<UnknownLane>> source, bool isPro)
        {
            var newTrack = new BasicInstrumentTrack2<DrumNote2<FourLane>>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertToFourLane(source, newTrack, isPro);
        }

        public static BasicInstrumentTrack2<DrumNote2<FiveLane>> ConvertToFiveLane(this BasicInstrumentTrack2<DrumNote2<UnknownLane>> source)
        {
            var newTrack = new BasicInstrumentTrack2<DrumNote2<FiveLane>>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertToFiveLane(source, newTrack);
        }

        public static BasicInstrumentTrack2<DrumNote2<FourLane>> ConvertToFourLane(this BasicInstrumentTrack2<DrumNote2<UnknownLane>> source, BasicInstrumentTrack2<DrumNote2<FourLane>> destination, bool isPro)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = diff.ConvertToFourLane(isPro);
                    source[i] = null;
                }
            }
            return destination;
        }

        public static BasicInstrumentTrack2<DrumNote2<FiveLane>> ConvertToFiveLane(this BasicInstrumentTrack2<DrumNote2<UnknownLane>> source, BasicInstrumentTrack2<DrumNote2<FiveLane>> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = diff.ConvertToFiveLane();
                    source[i] = null;
                }
            }
            return destination;
        }

        private unsafe static DifficultyTrack2<DrumNote2<FourLane>> ConvertToFourLane(this DifficultyTrack2<DrumNote2<UnknownLane>> source, bool isPro)
        {
            var newDifficulty = new DifficultyTrack2<DrumNote2<FourLane>>()
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };

            newDifficulty.Notes.Capacity = source.Notes.Count;
            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                if (!isPro)
                {
                    curr->Value.Pads.Yellow.CymbalFlag = false;
                    curr->Value.Pads.Blue.CymbalFlag = false;
                    curr->Value.Pads.Orange.CymbalFlag = false;
                }
                newDifficulty.Notes.Append(curr->Key, in *(DrumNote2<FourLane>*) &curr->Value);
            }
            source.Notes.Dispose();
            return newDifficulty;
        }

        private unsafe static DifficultyTrack2<DrumNote2<FiveLane>> ConvertToFiveLane(this DifficultyTrack2<DrumNote2<UnknownLane>> source)
        {
            var newDifficulty = new DifficultyTrack2<DrumNote2<FiveLane>>()
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };

            newDifficulty.Notes.Capacity = source.Notes.Count;
            var buffer = default(DrumNote2<FiveLane>);
            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                buffer.Bass = curr->Value.Bass;
                buffer.IsDoubleBass = curr->Value.IsDoubleBass;
                buffer.IsFlammed = curr->Value.IsFlammed;
                buffer.Pads.Snare = *(DrumPad*) &curr->Value.Pads.Snare;
                buffer.Pads.Yellow = *(DrumPad*) &curr->Value.Pads.Yellow;
                buffer.Pads.Blue = *(DrumPad*) &curr->Value.Pads.Blue;
                buffer.Pads.Orange = *(DrumPad*) &curr->Value.Pads.Orange;
                buffer.Pads.Green = *(DrumPad*) &curr->Value.Pads.Green;
                newDifficulty.Notes.Append(curr->Key, in buffer);
            }
            source.Notes.Dispose();
            return newDifficulty;
        }
    }
}
