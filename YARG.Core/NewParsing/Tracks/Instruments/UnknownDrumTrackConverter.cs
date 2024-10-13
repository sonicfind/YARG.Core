using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static InstrumentTrack2<FourLaneDrums> ConvertToFourLane(this InstrumentTrack2<UnknownLaneDrums> source, bool isPro)
        {
            var newTrack = new InstrumentTrack2<FourLaneDrums>()
            {
                Events = source.Events,
            };
            return ConvertToFourLane(source, newTrack, isPro);
        }

        public static InstrumentTrack2<FiveLaneDrums> ConvertToFiveLane(this InstrumentTrack2<UnknownLaneDrums> source)
        {
            var newTrack = new InstrumentTrack2<FiveLaneDrums>()
            {
                Events = source.Events,
            };
            return ConvertToFiveLane(source, newTrack);
        }

        public static InstrumentTrack2<FourLaneDrums> ConvertToFourLane(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FourLaneDrums> destination, bool isPro)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                ref var diff = ref source.Difficulties[i];
                if (!diff.IsEmpty())
                {
                    destination.Difficulties[i] = diff.ConvertToFourLane(isPro);
                    diff = DifficultyTrack2<UnknownLaneDrums>.Default;
                }
            }
            return destination;
        }

        public static InstrumentTrack2<FiveLaneDrums> ConvertToFiveLane(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FiveLaneDrums> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                ref var diff = ref source.Difficulties[i];
                if (!diff.IsEmpty())
                {
                    destination.Difficulties[i] = diff.ConvertToFiveLane();
                    diff = DifficultyTrack2<UnknownLaneDrums>.Default;
                }
            }
            return destination;
        }

        private static unsafe DifficultyTrack2<FourLaneDrums> ConvertToFourLane(this DifficultyTrack2<UnknownLaneDrums> source, bool isPro)
        {
            var newDifficulty = new DifficultyTrack2<FourLaneDrums>()
            {
                Overdrives = source.Overdrives,
                Soloes = source.Soloes,
                Trills = source.Trills,
                Tremolos = source.Tremolos,
                BREs = source.BREs,
                Faceoff_Player1 = source.Faceoff_Player1,
                Faceoff_Player2 = source.Faceoff_Player2,
                Events = source.Events,
            };
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
                newDifficulty.Notes.Append(in curr->Key, in *(FourLaneDrums*) &curr->Value);
            }
            source.Notes.Dispose();
            return newDifficulty;
        }

        private static unsafe DifficultyTrack2<FiveLaneDrums> ConvertToFiveLane(this DifficultyTrack2<UnknownLaneDrums> source)
        {
            const int DUAL_COUNT = 5;
            const int DYMANICS_COUNT = 4;
            var newDifficulty = new DifficultyTrack2<FiveLaneDrums>()
            {
                Overdrives = source.Overdrives,
                Soloes = source.Soloes,
                Trills = source.Trills,
                Tremolos = source.Tremolos,
                BREs = source.BREs,
                Faceoff_Player1 = source.Faceoff_Player1,
                Faceoff_Player2 = source.Faceoff_Player2,
                Events = source.Events,
            };
            newDifficulty.Notes.Capacity = source.Notes.Count;

            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                var note = newDifficulty.Notes.Append(in curr->Key);
                //                                          Bass + four pads = 5
                Unsafe.CopyBlock(&note->Kick, &curr->Value.Kick, DUAL_COUNT * (uint) sizeof(DualTime));
                note->Green = curr->Value.Green;

                //                                                                   four pads = 4 (duh)
                Unsafe.CopyBlock(&note->Dynamics_Snare, &curr->Value.Dynamics_Snare, DYMANICS_COUNT * (uint) sizeof(DrumDynamics));
                note->Dynamics_Green = curr->Value.Dynamics_Green;

                note->KickState = curr->Value.KickState;
                note->IsFlammed = curr->Value.IsFlammed;
            }
            source.Notes.Dispose();
            return newDifficulty;
        }
    }
}
