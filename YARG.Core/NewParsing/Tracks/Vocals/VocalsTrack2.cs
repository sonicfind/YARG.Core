using System;
using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public abstract class VocalTrack2 : ITrack
    {
        public YARGNativeSortedList<DualTime, bool> Percussion = YARGNativeSortedList<DualTime, bool>.Default;
        public YARGNativeSortedList<DualTime, DualTime> VocalPhrases_1 = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime> VocalPhrases_2 = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime> HarmonyLines = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime> RangeShifts = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime> Overdrives = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedSet<DualTime> LyricShifts = YARGNativeSortedSet<DualTime>.Default;
        public YARGManagedSortedList<DualTime, HashSet<string>> Events = YARGManagedSortedList<DualTime, HashSet<string>>.Default;

        /// <summary>
        /// Returns the vocal part (lyrics and notes) for the specified index
        /// </summary>
        /// <param name="index">The part index</param>
        /// <returns>The notes and lyrics for a specific vocal part</returns>
        public abstract ref VocalPart2 this[int index] { get; }
        public abstract int NumTracks { get; }

        protected VocalTrack2() { }

        /// <summary>
        /// Returns whether all parts, percussion, phrases, and events are empty
        /// </summary>
        /// <returns>If all containers are empty, return true</returns>
        public virtual bool IsEmpty()
        {
            return Percussion.IsEmpty()
                && VocalPhrases_1.IsEmpty()
                && VocalPhrases_2.IsEmpty()
                && HarmonyLines.IsEmpty()
                && RangeShifts.IsEmpty()
                && Overdrives.IsEmpty()
                && LyricShifts.IsEmpty()
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all parts, percussion, phrase, and events
        /// </summary>
        public virtual void Clear()
        {
            Percussion.Clear();
            VocalPhrases_1.Clear();
            VocalPhrases_2.Clear();
            HarmonyLines.Clear();
            RangeShifts.Clear();
            Overdrives.Clear();
            LyricShifts.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Trims the buffers for parts, percussion, and phrases
        /// </summary>
        public virtual void TrimExcess()
        {
            if ((Percussion.Count < 20 || 400 <= Percussion.Count) && Percussion.Count < Percussion.Capacity)
            {
                Percussion.TrimExcess();
            }
            VocalPhrases_1.TrimExcess();
            VocalPhrases_2.TrimExcess();
            HarmonyLines.TrimExcess();
            RangeShifts.TrimExcess();
            Overdrives.TrimExcess();
            LyricShifts.TrimExcess();
        }

        /// <summary>
        /// Returns the latest time when any part (or percussion) ends
        /// </summary>
        /// <returns>The latest end time of the track</returns>
        public virtual void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            if (Percussion.IsEmpty())
            {
                return;
            }

            unsafe
            {
                ref readonly var perc = ref Percussion.Data[Percussion.Count - 1];
                if (perc.Key > lastNoteTime)
                {
                    lastNoteTime = perc.Key;
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged buffer data for each part, percussion, and phrases
        /// </summary>
        protected virtual void _Dispose()
        {
            Percussion.Dispose();
            VocalPhrases_1.Dispose();
            VocalPhrases_2.Dispose();
            HarmonyLines.Dispose();
            RangeShifts.Dispose();
            Overdrives.Dispose();
            LyricShifts.Dispose();
        }

        /// <summary>
        /// Disposes all unmanaged buffer data for each part, percussion, and phrases
        /// </summary>
        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        ~VocalTrack2()
        {
            _Dispose();
        }
    }

    public class LeadVocalsTrack : VocalTrack2
    {
        private VocalPart2 _part = VocalPart2.Default;

        /// <summary>
        /// Returns the lead vocal part when given index 0
        /// </summary>
        /// <param name="index">The part index</param>
        /// <returns>The notes and lyrics for lead vocals</returns>
        public override ref VocalPart2 this[int index]
        {
            get
            {
                if (index > 0)
                {
                    throw new System.IndexOutOfRangeException();
                }
                return ref _part;
            }
        }

        public override int NumTracks => 1;

        /// <summary>
        /// Returns whether lead vocal part, percussion, phrases, and events are empty
        /// </summary>
        /// <returns>If all containers are empty, return true</returns>
        public override bool IsEmpty()
        {
            return _part.IsEmpty() && base.IsEmpty();
        }

        /// <summary>
        /// Clears all notes, lyrics, percussion, phrase, and events
        /// </summary>
        public override void Clear()
        {
            _part.Clear();
            base.Clear();
        }

        /// <summary>
        /// Trims the buffers for the lead's notes, percussion, and phrases
        /// </summary>
        public override void TrimExcess()
        {
            _part.TrimExcess();
        }

        /// <summary>
        /// Returns the latest time when lead vocals or percussion ends
        /// </summary>
        /// <returns>The latest end time of the track</returns>
        public override void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            base.UpdateLastNoteTime(ref lastNoteTime);
            _part.UpdateLastNoteTime(ref lastNoteTime);
        }

        /// <summary>
        /// Disposes all unmanaged buffer data for lead's notes and lyrics, percussion, and phrases
        /// </summary>
        protected override void _Dispose()
        {
            _part.Notes.Dispose();
            _part.Lyrics.Clear();
            base._Dispose();
        }
    }

    public class HarmonyVocalsTrack : VocalTrack2
    {
        public VocalPart2 Harm_1 = VocalPart2.Default;
        public VocalPart2 Harm_2 = VocalPart2.Default;
        public VocalPart2 Harm_3 = VocalPart2.Default;

        /// <summary>
        /// Returns the vocal part (lyrics and notes) for the specified index [0,1,2]
        /// </summary>
        /// <param name="index">The part index</param>
        /// <returns>The notes and lyrics for a specific vocal part</returns>
        public override ref VocalPart2 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return ref Harm_1;
                    case 1: return ref Harm_2;
                    case 2: return ref Harm_3;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public override int NumTracks => 3;

        /// <summary>
        /// Returns whether all parts, percussion, phrases, and events are empty
        /// </summary>
        /// <returns>If all containers are empty, return true</returns>
        public override bool IsEmpty()
        {
            return Harm_1.IsEmpty()
                && Harm_2.IsEmpty()
                && Harm_3.IsEmpty()
                && base.IsEmpty();
        }

        /// <summary>
        /// Clears all parts, percussion, phrase, and events
        /// </summary>
        public override void Clear()
        {
            Harm_1.Clear();
            Harm_2.Clear();
            Harm_3.Clear();
            base.Clear();
        }

        /// <summary>
        /// Trims the buffers for parts, percussion, and phrases
        /// </summary>
        public override void TrimExcess()
        {
            Harm_1.TrimExcess();
            Harm_2.TrimExcess();
            Harm_3.TrimExcess();
        }

        /// <summary>
        /// Returns the latest time when any part (or percussion) ends
        /// </summary>
        /// <returns>The latest end time of the track</returns>
        public override void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            base.UpdateLastNoteTime(ref lastNoteTime);
            Harm_1.UpdateLastNoteTime(ref lastNoteTime);
            Harm_2.UpdateLastNoteTime(ref lastNoteTime);
            Harm_3.UpdateLastNoteTime(ref lastNoteTime);
        }

        /// <summary>
        /// Disposes all unmanaged buffer data for each part, percussion, and phrases
        /// </summary>
        protected override void _Dispose()
        {
            Harm_1.Dispose();
            Harm_2.Dispose();
            Harm_3.Dispose();
            base._Dispose();
        }
    }
}
