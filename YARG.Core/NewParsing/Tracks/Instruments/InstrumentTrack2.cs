using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class InstrumentTrack2<TDifficultyTrack> : Track
        where TDifficultyTrack : Track
    {
        protected readonly TDifficultyTrack?[] difficulties = new TDifficultyTrack[4];
        public override bool IsOccupied()
        {
            foreach (var diff in difficulties)
            {
                if (diff != null && diff.IsOccupied())
                {
                    return true;
                }
            }
            return base.IsOccupied();
        }

        public override void Clear()
        {
            base.Clear();
            foreach (var diff in difficulties)
            {
                diff?.Clear();
            }
        }

        public override void TrimExcess()
        {
            foreach (var diff in difficulties)
            {
                diff?.TrimExcess();
            }
        }

        public ref TDifficultyTrack? this[int index] { get { return ref difficulties[index]; } }
        public ref TDifficultyTrack? this[Difficulty diff] { get { return ref difficulties[(int) diff - 1]; } }

        public override DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
            foreach (var diff in difficulties)
            {
                if (diff != null)
                {
                    var end = diff.GetLastNoteTime();
                    if (end > endTime)
                    {
                        endTime = end;
                    }
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
                    foreach (var diff in difficulties)
                    {
                        diff?.Dispose();
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}
