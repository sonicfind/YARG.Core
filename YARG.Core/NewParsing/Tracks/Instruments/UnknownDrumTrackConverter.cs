using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static void ConvertTo(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FourLaneDrums> destination, bool isPro)
        {
            destination.Events = source.Events;
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                ref var diff = ref source.Difficulties[i];
                if (!diff.IsEmpty())
                {
                    diff.ConvertTo(ref destination.Difficulties[i], isPro);
                    diff = DifficultyTrack2<UnknownLaneDrums>.Default;
                }
            }
        }

        public static void ConvertTo(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FiveLaneDrums> destination)
        {
            destination.Events = source.Events;
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                ref var diff = ref source.Difficulties[i];
                if (!diff.IsEmpty())
                {
                    diff.ConvertTo(ref destination.Difficulties[i]);
                    diff = DifficultyTrack2<UnknownLaneDrums>.Default;
                }
            }
        }

        private static unsafe void ConvertTo(this DifficultyTrack2<UnknownLaneDrums> source, ref DifficultyTrack2<FourLaneDrums> destination, bool isPro)
        {
            destination.Overdrives = source.Overdrives;
            destination.Soloes = source.Soloes;
            destination.Trills = source.Trills;
            destination.Tremolos = source.Tremolos;
            destination.BREs = source.BREs;
            destination.Faceoff_Player1 = source.Faceoff_Player1;
            destination.Faceoff_Player2 = source.Faceoff_Player2;
            destination.Events = source.Events;

            destination.Notes.Capacity = source.Notes.Count;
            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                if (!isPro)
                {
                    curr->Value.Cymbal_Yellow = false;
                    curr->Value.Cymbal_Blue = false;
                    curr->Value.Cymbal_Orange = false;
                }
                destination.Notes.Append(in curr->Key, in *(FourLaneDrums*) &curr->Value);
            }
            source.Notes.Dispose();
        }

        private static unsafe void ConvertTo(this DifficultyTrack2<UnknownLaneDrums> source, ref DifficultyTrack2<FiveLaneDrums> destination)
        {
            const int DUAL_COUNT = 5;
            const int DYMANICS_COUNT = 4;
            destination.Overdrives = source.Overdrives;
            destination.Soloes = source.Soloes;
            destination.Trills = source.Trills;
            destination.Tremolos = source.Tremolos;
            destination.BREs = source.BREs;
            destination.Faceoff_Player1 = source.Faceoff_Player1;
            destination.Faceoff_Player2 = source.Faceoff_Player2;
            destination.Events = source.Events;

            destination.Notes.Capacity = source.Notes.Count;
            var end = source.Notes.End;
            for (var curr = source.Notes.Data; curr < end; ++curr)
            {
                var note = destination.Notes.Append(in curr->Key);
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
        }
    }
}
