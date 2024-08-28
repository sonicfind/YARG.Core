using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProGuitarDifficultyTrack<TProFret> : DifficultyTrack2<ProGuitarNote<TProFret>>
        where TProFret : unmanaged, IProFret
    {
        public readonly YARGNativeSortedList<DualTime, DualTime> Arpeggios = new();

        /// <summary>
        /// Returns whether the track contains no notes, arpeggios, phrases, nor events
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public override bool IsEmpty()
        {
            return Arpeggios.IsEmpty() && base.IsEmpty();
        }

        /// <summary>
        /// Clears all notes, arpeggios, phrases, and events
        /// </summary>
        public override void Clear()
        {
            Arpeggios.Clear();
            base.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from notes, arpeggios, and phrases
        /// </summary>
        public override void TrimExcess()
        {
            Arpeggios.TrimExcess();
            base.TrimExcess();
        }

        /// <summary>
        /// Diposes all unmanagaed buffer data of notes, arpeggios, and phrases
        /// </summary>
        public override void Dispose()
        {
            Arpeggios.Dispose();
            base.Dispose();
        }
    }
}
