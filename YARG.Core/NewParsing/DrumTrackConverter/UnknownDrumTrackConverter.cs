﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static InstrumentTrack2<DifficultyTrack2<FourLaneDrums>> ConvertToFourLane(this InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>> source, bool isPro)
        {
            var newTrack = new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>(source);
            return ConvertToFourLane(source, newTrack, isPro);
        }

        public static InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>> ConvertToFiveLane(this InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>> source)
        {
            var newTrack = new InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>>(source);
            return ConvertToFiveLane(source, newTrack);
        }

        public static InstrumentTrack2<DifficultyTrack2<FourLaneDrums>> ConvertToFourLane(this InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>> source, InstrumentTrack2<DifficultyTrack2<FourLaneDrums>> destination, bool isPro)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source.Difficulties[i];
                if (diff != null && !diff.IsEmpty())
                {
                    destination.Difficulties[i] = diff.ConvertToFourLane(isPro);
                    source.Difficulties[i] = null;
                }
            }
            return destination;
        }

        public static InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>> ConvertToFiveLane(this InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>> source, InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                var diff = source.Difficulties[i];
                if (diff != null && !diff.IsEmpty())
                {
                    destination.Difficulties[i] = diff.ConvertToFiveLane();
                    source.Difficulties[i] = null;
                }
            }
            return destination;
        }

        private static unsafe DifficultyTrack2<FourLaneDrums> ConvertToFourLane(this DifficultyTrack2<UnknownLaneDrums> source, bool isPro)
        {
            var newDifficulty = new DifficultyTrack2<FourLaneDrums>(source);

            newDifficulty.Notes.Capacity = source.Notes.Count;
            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                if (!isPro)
                {
                    curr->Value.Cymbal_Yellow = false;
                    curr->Value.Cymbal_Blue = false;
                    curr->Value.Cymbal_Orange = false;
                }
                newDifficulty.Notes.Append(curr->Key, in *(FourLaneDrums*) &curr->Value);
            }
            source.Notes.Dispose();
            return newDifficulty;
        }

        private static unsafe DifficultyTrack2<FiveLaneDrums> ConvertToFiveLane(this DifficultyTrack2<UnknownLaneDrums> source)
        {
            var newDifficulty = new DifficultyTrack2<FiveLaneDrums>(source);
            newDifficulty.Notes.Capacity = source.Notes.Count;
            var buffer = default(FiveLaneDrums);
            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                buffer.Bass = curr->Value.Bass;
                buffer.IsDoubleBass = curr->Value.IsDoubleBass;
                buffer.IsFlammed = curr->Value.IsFlammed;
                buffer.Snare = curr->Value.Snare;
                buffer.Yellow = curr->Value.Yellow;
                buffer.Blue = curr->Value.Blue;
                buffer.Orange = curr->Value.Orange;
                buffer.Green = curr->Value.Green;
                newDifficulty.Notes.Append(curr->Key, in buffer);
            }
            source.Notes.Dispose();
            return newDifficulty;
        }
    }
}
