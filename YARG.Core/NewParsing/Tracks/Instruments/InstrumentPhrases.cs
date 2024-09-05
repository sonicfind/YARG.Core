using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public sealed class InstrumentPhrases
    {
        public readonly YARGNativeSortedList<DualTime, DualTime> Overdrives;
        public readonly YARGNativeSortedList<DualTime, DualTime> Soloes;
        public readonly YARGNativeSortedList<DualTime, DualTime> Trills;
        public readonly YARGNativeSortedList<DualTime, DualTime> Tremolos;
        public readonly YARGNativeSortedList<DualTime, DualTime> BREs;
        public readonly YARGNativeSortedList<DualTime, DualTime> Faceoff_Player1;
        public readonly YARGNativeSortedList<DualTime, DualTime> Faceoff_Player2;

        public InstrumentPhrases()
        {
            Overdrives = new();
            Soloes = new();
            Trills = new();
            Tremolos = new();
            BREs = new();
            Faceoff_Player1 = new();
            Faceoff_Player2 = new();
        }

        /// <summary>
        /// Move constructor that pulls all the phrases and events from the source into the new track
        /// </summary>
        /// <param name="source">The track to be left in a default state after the call</param>
        public InstrumentPhrases(InstrumentPhrases source)
        {
            Overdrives      = new(source.Overdrives);
            Soloes          = new(source.Soloes);
            Trills          = new(source.Trills);
            Tremolos        = new(source.Tremolos);
            BREs            = new(source.BREs);
            Faceoff_Player1 = new(source.Faceoff_Player1);
            Faceoff_Player2 = new(source.Faceoff_Player2);
        }

        /// <summary>
        /// Returns if no phrases nor events are present
        /// </summary>
        public bool IsEmpty()
        {
            return Overdrives.IsEmpty()
                && Soloes.IsEmpty()
                && Trills.IsEmpty()
                && Tremolos.IsEmpty()
                && BREs.IsEmpty()
                && Faceoff_Player1.IsEmpty()
                && Faceoff_Player2.IsEmpty();
        }

        /// <summary>
        /// Clears all phrases and events
        /// </summary>
        public void Clear()
        {
            Overdrives.Clear();
            Soloes.Clear();
            Trills.Clear();
            Tremolos.Clear();
            BREs.Clear();
            Faceoff_Player1.Clear();
            Faceoff_Player2.Clear();
        }

        /// <summary>
        /// Trims excess data from all phrase containers
        /// </summary>
        public void TrimExcess()
        {
            Overdrives.TrimExcess();
            Soloes.TrimExcess();
            Trills.TrimExcess();
            Tremolos.TrimExcess();
            BREs.TrimExcess();
            Faceoff_Player1.TrimExcess();
            Faceoff_Player2.TrimExcess();
        }

        /// <summary>
        /// Dispose the unmanaged buffer for each phrase container
        /// </summary>
        public void Dispose()
        {
            Overdrives.Dispose();
            Soloes.Dispose();
            Trills.Dispose();
            Tremolos.Dispose();
            BREs.Dispose();
            Faceoff_Player1.Dispose();
            Faceoff_Player2.Dispose();
        }
    }
}
