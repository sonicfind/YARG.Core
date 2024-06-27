using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static BasicInstrumentTrack2<DrumNote2<TDrumConfig, DrumPad>> Convert<TDrumConfig>(this BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> source)
            where TDrumConfig : unmanaged, IDrumPadConfig<DrumPad>
        {
            var newTrack = new BasicInstrumentTrack2<DrumNote2<TDrumConfig, DrumPad>>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertTo(source, newTrack);
        }

        public static BasicInstrumentTrack2<DrumNote2<TDrumConfig, DrumPad>> ConvertTo<TDrumConfig>(this BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> source, BasicInstrumentTrack2<DrumNote2<TDrumConfig, DrumPad>> destination)
            where TDrumConfig : unmanaged, IDrumPadConfig<DrumPad>
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = diff.Convert<TDrumConfig>();
                    source[i] = null;
                }
            }
            return destination;
        }

        private static DifficultyTrack2<DrumNote2<TDrumConfig, DrumPad>> Convert<TDrumConfig>(this DifficultyTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> source)
            where TDrumConfig : unmanaged, IDrumPadConfig<DrumPad>
        {
            var newDifficulty = new DifficultyTrack2<DrumNote2<TDrumConfig, DrumPad>>()
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };

            newDifficulty.Notes.Capacity = source.Notes.Count;
            unsafe
            {
                var buffer = default(DrumNote2<TDrumConfig, DrumPad>);
                var end = source.Notes.End;
                for (var curr = source.Notes.Data; curr < end; ++curr)
                {
                    ref readonly var val = ref curr->Value;
                    buffer.Bass = val.Bass;
                    buffer.IsDoubleBass = val.IsDoubleBass;
                    buffer.IsFlammed = val.IsFlammed;
                    for (int i = 0; i < buffer.Pads.NumPads; ++i)
                    {
                        ref var pad = ref buffer.Pads[i];
                        pad.Duration = curr->Value.Pads[i].Duration;
                        pad.Dynamics = curr->Value.Pads[i].Dynamics;
                    }
                    newDifficulty.Notes.Append(curr->Key, in buffer);
                }
            }
            source.Notes.Dispose();
            return newDifficulty;
        }

        public static BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>> Convert(this BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> source)
        {
            var newTrack = new BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>>
            {
                SpecialPhrases = source.SpecialPhrases,
                Events = source.Events
            };
            return ConvertTo(source, newTrack);
        }

        public static BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>> ConvertTo(this BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> source, BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source[i];
                if (diff != null && diff.IsOccupied())
                {
                    destination[i] = diff.Convert();
                    source[i] = null;
                }
            }
            return destination;
        }

        private static DifficultyTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>> Convert(this DifficultyTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> source)
        {
            var newDifficulty = new DifficultyTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>>()
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
                    newDifficulty.Notes.Append(curr->Key, in *(DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>*)&curr->Value);
                }
            }
            source.Notes.Dispose();
            return newDifficulty;
        }
    }
}
