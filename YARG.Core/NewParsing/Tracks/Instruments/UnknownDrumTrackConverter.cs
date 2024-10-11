using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static InstrumentTrack2<FourLaneDrums> ConvertToFourLane(this InstrumentTrack2<UnknownLaneDrums> source, bool isPro)
        {
            var newTrack = new InstrumentTrack2<FourLaneDrums>();
            newTrack.Events.StealData(source.Events);
            return ConvertToFourLane(source, newTrack, isPro);
        }

        public static InstrumentTrack2<FiveLaneDrums> ConvertToFiveLane(this InstrumentTrack2<UnknownLaneDrums> source)
        {
            var newTrack = new InstrumentTrack2<FiveLaneDrums>();
            newTrack.Events.StealData(source.Events);
            return ConvertToFiveLane(source, newTrack);
        }

        public static InstrumentTrack2<FourLaneDrums> ConvertToFourLane(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FourLaneDrums> destination, bool isPro)
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

        public static InstrumentTrack2<FiveLaneDrums> ConvertToFiveLane(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FiveLaneDrums> destination)
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
            var newDifficulty = new DifficultyTrack2<FourLaneDrums>();
            newDifficulty.Overdrives.StealData(source.Overdrives);
            newDifficulty.Soloes.StealData(source.Soloes);
            newDifficulty.Trills.StealData(source.Trills);
            newDifficulty.Tremolos.StealData(source.Tremolos);
            newDifficulty.BREs.StealData(source.BREs);
            newDifficulty.Faceoff_Player1.StealData(source.Faceoff_Player1);
            newDifficulty.Faceoff_Player2.StealData(source.Faceoff_Player2);
            newDifficulty.Events.StealData(source.Events);
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
            var newDifficulty = new DifficultyTrack2<FiveLaneDrums>();
            newDifficulty.Overdrives.StealData(source.Overdrives);
            newDifficulty.Soloes.StealData(source.Soloes);
            newDifficulty.Trills.StealData(source.Trills);
            newDifficulty.Tremolos.StealData(source.Tremolos);
            newDifficulty.BREs.StealData(source.BREs);
            newDifficulty.Faceoff_Player1.StealData(source.Faceoff_Player1);
            newDifficulty.Faceoff_Player2.StealData(source.Faceoff_Player2);
            newDifficulty.Events.StealData(source.Events);
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
