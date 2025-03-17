using System.Collections.Generic;
using YARG.Core.Containers;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public class DifficultyTrack2<TNote> : ITrack
        where TNote : unmanaged, IInstrumentNote
    {
        public YargNativeSortedList<DualTime, TNote> Notes { get; }
        public YargNativeSortedList<DualTime, DualTime> Overdrives { get; }
        public YargNativeSortedList<DualTime, DualTime> Solos { get; }
        public YargNativeSortedList<DualTime, DualTime> Trills { get; }
        public YargNativeSortedList<DualTime, DualTime> Tremolos { get; }
        public YargNativeSortedList<DualTime, DualTime> BREs { get; }
        public YargNativeSortedList<DualTime, DualTime> FaceOffPlayer1 { get; }
        public YargNativeSortedList<DualTime, DualTime> FaceOffPlayer2 { get; }
        public YargManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public DifficultyTrack2()
        {
            Notes          = new YargNativeSortedList<DualTime, TNote>();
            Overdrives     = new YargNativeSortedList<DualTime, DualTime>();
            Solos          = new YargNativeSortedList<DualTime, DualTime>();
            Trills         = new YargNativeSortedList<DualTime, DualTime>();
            Tremolos       = new YargNativeSortedList<DualTime, DualTime>();
            BREs           = new YargNativeSortedList<DualTime, DualTime>();
            FaceOffPlayer1 = new YargNativeSortedList<DualTime, DualTime>();
            FaceOffPlayer2 = new YargNativeSortedList<DualTime, DualTime>();
            Events         = new YargManagedSortedList<DualTime, HashSet<string>>();
        }

        public DifficultyTrack2(DifficultyTrack2<TNote> source)
        {
            Notes          = new YargNativeSortedList<DualTime, TNote>(source.Notes);
            Overdrives     = new YargNativeSortedList<DualTime, DualTime>(source.Overdrives);
            Solos          = new YargNativeSortedList<DualTime, DualTime>(source.Solos);
            Trills         = new YargNativeSortedList<DualTime, DualTime>(source.Trills);
            Tremolos       = new YargNativeSortedList<DualTime, DualTime>(source.Tremolos);
            BREs           = new YargNativeSortedList<DualTime, DualTime>(source.BREs);
            FaceOffPlayer1 = new YargNativeSortedList<DualTime, DualTime>(source.FaceOffPlayer1);
            FaceOffPlayer2 = new YargNativeSortedList<DualTime, DualTime>(source.FaceOffPlayer2);
            Events         = new YargManagedSortedList<DualTime, HashSet<string>>(source.Events);
        }

        public void CopyFrom(DifficultyTrack2<TNote> source)
        {
            Notes         .CopyFrom(source.Notes);
            Overdrives    .CopyFrom(source.Overdrives);
            Solos         .CopyFrom(source.Solos);
            Trills        .CopyFrom(source.Trills);
            Tremolos      .CopyFrom(source.Tremolos);
            BREs          .CopyFrom(source.BREs);
            FaceOffPlayer1.CopyFrom(source.FaceOffPlayer1);
            FaceOffPlayer2.CopyFrom(source.FaceOffPlayer2);
            Events        .CopyFrom(source.Events);
        }

        /// <summary>
        /// Returns if no notes, phrases, or events are present
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public bool IsEmpty()
        {
            return Notes.IsEmpty()
                && Overdrives.IsEmpty()
                && Solos.IsEmpty()
                && Trills.IsEmpty()
                && Tremolos.IsEmpty()
                && BREs.IsEmpty()
                && FaceOffPlayer1.IsEmpty()
                && FaceOffPlayer2.IsEmpty()
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all notes, phrases, and events
        /// </summary>
        public void Clear()
        {
            Notes.Clear();
            Overdrives.Clear();
            Solos.Clear();
            Trills.Clear();
            Tremolos.Clear();
            BREs.Clear();
            FaceOffPlayer1.Clear();
            FaceOffPlayer2.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from notes and phrases
        /// </summary>
        public void TrimExcess()
        {
            if ((Notes.Count < 500 || 10000 <= Notes.Count) && Notes.Count < Notes.Capacity)
            {
                Notes.TrimExcess();
            }
            Overdrives.TrimExcess();
            Solos.TrimExcess();
            Trills.TrimExcess();
            Tremolos.TrimExcess();
            BREs.TrimExcess();
            FaceOffPlayer1.TrimExcess();
            FaceOffPlayer2.TrimExcess();
            // Trimming managed lists just generates a new array for GC to handle.
            // The exact opposite of what we want.
        }

        public void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            if (!Notes.IsEmpty())
            {
                ref readonly var note = ref Notes[Notes.Count - 1];
                var tmp = note.Key + note.Value.GetLongestSustain();
                if (tmp > lastNoteTime)
                {
                    lastNoteTime = tmp;
                }
            }
        }

        public HashWrapper ComputeHash()
        {
            var hash = HashWrapper.Hash(Notes.SpanAsBytes);
            using var buffer = new YargNativeList<byte>();
            UpdateHash(Overdrives, 1);
            UpdateHash(Solos, 2);
            UpdateHash(Trills, 3);
            UpdateHash(Tremolos, 4);
            UpdateHash(BREs, 5);
            UpdateHash(FaceOffPlayer1, 6);
            UpdateHash(FaceOffPlayer2, 7);
            return hash;

            void UpdateHash(YargNativeList<(DualTime, DualTime)> list, int indexTag)
            {
                buffer.Clear();
                long byteCount = list.CountInBytes;
                if (buffer.Capacity < byteCount + 1)
                {
                    buffer.Capacity = byteCount + 1;
                }

                buffer.Add((byte)indexTag);
                unsafe
                {
                    buffer.AddRange((byte*)list.Data, byteCount);
                }
                hash ^= HashWrapper.Hash(buffer.Span);
            }
        }

        public void Dispose()
        {
            Notes.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
            Trills.Dispose();
            Tremolos.Dispose();
            BREs.Dispose();
            FaceOffPlayer1.Dispose();
            FaceOffPlayer2.Dispose();
            Events.Dispose();
        }
    }
}
