using System.Runtime.CompilerServices;

namespace YARG.Core.NewParsing
{
    internal static class UnknownDrumTrackConverter
    {
        public static InstrumentTrack2<FourLaneDrums> ConvertToFourLane(this InstrumentTrack2<UnknownLaneDrums> source)
        {
            var destination = new InstrumentTrack2<FourLaneDrums>();
            Convert(source, destination);
            return destination;
        }

        public static InstrumentTrack2<FiveLaneDrums> ConvertToFiveLane(this InstrumentTrack2<UnknownLaneDrums> source)
        {
            var destination = new InstrumentTrack2<FiveLaneDrums>();
            Convert(source, destination);
            return destination;
        }

        public static void Convert(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FourLaneDrums> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                if (destination[i].IsEmpty())
                {
                    source[i].Convert(destination[i]);
                }
            }
            destination.Events.MoveFrom(source.Events);
        }

        public static void Convert(this InstrumentTrack2<UnknownLaneDrums> source, InstrumentTrack2<FiveLaneDrums> destination)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                if (destination[i].IsEmpty())
                {
                    source[i].Convert(destination[i]);
                }
            }
            destination.Events.MoveFrom(source.Events);
        }

        private static void Convert(this DifficultyTrack2<UnknownLaneDrums> source, DifficultyTrack2<FourLaneDrums> destination)
        {
            destination.Notes.Capacity = source.Notes.Count;
            for (long i = 0; i < source.Notes.Count; ++i)
            {
                unsafe
                {
                    var curr = source.Notes.Data + i;
                    destination.Notes.Add(in curr->Key, in *(FourLaneDrums*) &curr->Value);
                }
            }
            source.Notes.Dispose();
            destination.Overdrives.MoveFrom(source.Overdrives);
            destination.Solos.MoveFrom(source.Solos);
            destination.Trills.MoveFrom(source.Trills);
            destination.Tremolos.MoveFrom(source.Tremolos);
            destination.BREs.MoveFrom(source.BREs);
            destination.FaceOffPlayer1.MoveFrom(source.FaceOffPlayer1);
            destination.FaceOffPlayer2.MoveFrom(source.FaceOffPlayer2);
            destination.Events.MoveFrom(source.Events);
        }

        private static void Convert(this DifficultyTrack2<UnknownLaneDrums> source, DifficultyTrack2<FiveLaneDrums> destination)
        {
            destination.Notes.Capacity = source.Notes.Count;
            for (long i = 0; i < source.Notes.Count; ++i)
            {
                unsafe
                {
                    const int DUAL_COUNT = 5;
                    const int DYMANICS_COUNT = 4;
                    var curr = source.Notes.Data + i;
                    var note = destination.Notes.Add(in curr->Key);
                    //                                               Bass + four pads = 5
                    Unsafe.CopyBlock(&note->Kick, &curr->Value.Kick, DUAL_COUNT * (uint) sizeof(DualTime));
                    note->Green = curr->Value.Green;

                    //                                                                   four pads = 4 (duh)
                    Unsafe.CopyBlock(&note->Dynamics_Snare, &curr->Value.Dynamics_Snare, DYMANICS_COUNT * (uint) sizeof(DrumDynamics));
                    note->Dynamics_Green = curr->Value.Dynamics_Green;

                    note->KickState = curr->Value.KickState;
                    note->IsFlammed = curr->Value.IsFlammed;
                }
            }
            source.Notes.Dispose();
            destination.Overdrives.MoveFrom(source.Overdrives);
            destination.Solos.MoveFrom(source.Solos);
            destination.Trills.MoveFrom(source.Trills);
            destination.Tremolos.MoveFrom(source.Tremolos);
            destination.BREs.MoveFrom(source.BREs);
            destination.FaceOffPlayer1.MoveFrom(source.FaceOffPlayer1);
            destination.FaceOffPlayer2.MoveFrom(source.FaceOffPlayer2);
            destination.Events.MoveFrom(source.Events);
        }
    }
}
