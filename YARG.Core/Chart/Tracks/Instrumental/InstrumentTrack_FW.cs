using System;

namespace YARG.Core.Chart
{
    public class InstrumentTrack_Base<TDifficultyTrack> : Track
        where TDifficultyTrack : Track, new()
    {
        protected readonly TDifficultyTrack?[] difficulties = new TDifficultyTrack[4];
        public override bool IsOccupied()
        {
            for (int i = 0; i < 4; ++i)
                if (difficulties[i] != null && difficulties[i]!.IsOccupied())
                    return true;
            return base.IsOccupied();
        }

        public override void Clear()
        {
            base.Clear();
            for (int i = 0; i < 4; ++i)
                difficulties[i]?.Clear();
        }

        public override void TrimExcess()
        {
            for (int i = 0; i < 4; ++i)
                difficulties[i]?.TrimExcess();
        }

        public ref TDifficultyTrack? this[int index] { get { return ref difficulties[index]; } }
        public ref TDifficultyTrack? this[Difficulty diff] { get { return ref difficulties[(int) diff - 1]; } }

        public override long GetLastNoteTime()
        {
            long endTime = 0;
            for (int i = 0; i < difficulties.Length; ++i)
            {
                if (difficulties[i] != null)
                {
                    long end = difficulties[i]!.GetLastNoteTime();
                    if (end > endTime)
                        endTime = end;
                }
            }
            return endTime;
        }

        public override void Dispose()
        {
            for (int i = 0; i < difficulties.Length; ++i)
            {
                ref var diff = ref difficulties[i];
                if (diff != null)
                {
                    diff.Dispose();
                    diff = null;
                }
            }
        }
    }

    public class InstrumentTrack_FW<T> : InstrumentTrack_Base<DifficultyTrack_FW<T>>
        where T : unmanaged, INote
    {
    }
}
