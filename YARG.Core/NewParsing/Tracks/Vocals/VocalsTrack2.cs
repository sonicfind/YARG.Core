using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class VocalTrack2 : Track
    {
        private readonly YARGManagedSortedList<DualTime, VocalNote2>[] _vocals;
        public readonly YARGNativeSortedList<DualTime, VocalPercussion2> Percussion = new();

        public YARGManagedSortedList<DualTime, VocalNote2> this[int trackIndex] => _vocals[trackIndex];

        public VocalTrack2(int numTracks)
        {
            _vocals = new YARGManagedSortedList<DualTime, VocalNote2>[numTracks];
            for (int i = 0; i < numTracks; i++)
                _vocals[i] = new();
        }

        public override bool IsEmpty()
        {
            foreach (var track in _vocals)
            {
                if (!track.IsEmpty())
                {
                    return false;
                }
            }
            return Percussion.IsEmpty() && base.IsEmpty();
        }

        public override void Clear()
        {
            foreach (var track in _vocals)
            {
                track.Clear();
            }
            Percussion.Clear();
            base.Clear();
        }

        public override void TrimExcess()
        {
            foreach (var track in _vocals)
            {
                if ((track.Count < 100 || 2000 <= track.Count) && track.Count < track.Capacity)
                {
                    track.TrimExcess();
                }
            }

            if ((Percussion.Count < 20 || 400 <= Percussion.Count) && Percussion.Count < Percussion.Capacity)
            {
                Percussion.TrimExcess();
            }
        }

        public override unsafe DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
            foreach (var track in _vocals)
            {
                if (track.IsEmpty())
                    continue;

                ref var vocal = ref track.ElementAtIndex(track.Count - 1);
                var end = vocal.Key + vocal.Value.Duration;
                if (end > endTime)
                {
                    endTime = end;
                }
            }

            if (!Percussion.IsEmpty())
            {
                var perc = Percussion.ElementAtIndex(Percussion.Count - 1);
                if (perc->Key > endTime)
                {
                    endTime = perc->Key;
                }
            }
            return endTime;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var track in _vocals)
                    {
                        track.Clear();
                    }
                    Percussion.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
