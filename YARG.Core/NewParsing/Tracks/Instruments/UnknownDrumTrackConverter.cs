using System;
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
            for (int i = 0; i < source.Notes.Count; ++i)
            {
                ref readonly var curr = ref source.Notes[i];
                destination.Notes.Add(in curr.Key, in curr.Value.FourLane);
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
                    var curr = source.Notes.Data + i;
                    var note = destination.Notes.Add(in curr->Key);
                    Buffer.MemoryCopy(&curr->Value.FourLane.Lanes, &note->Lanes, sizeof(FiveLaneDrums.LaneArray), sizeof(FourLaneDrums.LaneArray));
                    note->Lanes.Green = curr->Value.FifthLane;

                    Buffer.MemoryCopy(&curr->Value.FourLane.Dynamics, &note->Dynamics, sizeof(FiveLaneDrums.DynamicsArray), sizeof(FourLaneDrums.DynamicsArray));
                    note->Dynamics.Green = curr->Value.FifthDynamics;

                    note->KickState = curr->Value.FourLane.KickState;
                    note->IsFlammed = curr->Value.FourLane.IsFlammed;
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
