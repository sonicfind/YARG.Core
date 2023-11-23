using System;
using YARG.Core.Parsing.Vocal;

namespace YARG.Core.Parsing
{
    public class VocalTrack_FW : Track
    {
        private readonly TimedManagedFlatDictionary<VocalNote_FW>[] vocals;
        public readonly TimedNativeFlatDictionary<VocalPercussion> Percussion = new();
        

        public TimedManagedFlatDictionary<VocalNote_FW> this[int trackIndex]
        {
            get { return vocals[trackIndex]; }
        }

        public VocalTrack_FW(int numTracks)
        {
            vocals = new TimedManagedFlatDictionary<VocalNote_FW>[numTracks];
            for (int i = 0; i < numTracks; i++)
                vocals[i] = new();
        }

        public override bool IsOccupied()
        {
            for (int i = 0; i < vocals.Length; i++)
                if (!vocals[i].IsEmpty())
                    return true;
            return !Percussion.IsEmpty() || base.IsOccupied();
        }

        public override void Clear()
        {
            base.Clear();
            for (int i = 0; i < vocals.Length; i++)
                vocals[i].Clear();
            Percussion.Clear();
        }

        public override void TrimExcess()
        {
            for (int i = 0; i < vocals.Length; i++)
            {
                ref var track = ref vocals[i];
                if ((track.Count < 100 || 2000 <= track.Count) && track.Count < track.Capacity)
                    track.TrimExcess();
            }

            if ((Percussion.Count < 20 || 400 <= Percussion.Count) && Percussion.Count < Percussion.Capacity)
                Percussion.TrimExcess();
        }

        public override DualTime GetLastNoteTime()
        {
            var endTime = DualTime.Zero;
            foreach (var track in vocals)
            {
                if (track.IsEmpty())
                    continue;

                ref var vocal = ref track.At_index(track.Count - 1);
                var end = vocal.position + vocal.obj.Duration;
                if (end > endTime)
                    endTime = end;
            }

            if (!Percussion.IsEmpty())
            {
                ref var perc = ref Percussion.At_index(Percussion.Count - 1);
                if (perc.position > endTime)
                    endTime = perc.position;
            }
            return endTime;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Percussion.Dispose();
                    foreach (var track in vocals)
                        track.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
