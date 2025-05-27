using System;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct HittablePhrase
    {
        public static readonly HittablePhrase Unhittable = new(in DualTime.Inactive, in DualTime.Inactive)
        {
            TotalNotes = 0,
            HitCount = 0
        };

        public DualTime StartTime  { get; }
        public DualTime EndTime    { get; }
        public int      TotalNotes { get; set; }
        public int      HitCount   { get; private set; }

        public readonly bool IsActive()
        {
            return HitCount != -1;
        }

        public bool AddHit()
        {
            if (HitCount >= TotalNotes)
            {
                throw new InvalidOperationException("All notes in phrase have been already hit");
            }

            if (HitCount != -1)
            {
                HitCount++;
            }
            return HitCount == TotalNotes;
        }

        public void Disable()
        {
            HitCount = -1;
        }

        /// <summary>
        /// Resets the phrase to zero hits
        /// </summary>
        public void Reset()
        {
            HitCount = 0;
        }

        public HittablePhrase(in DualTime startTime, in DualTime endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
            TotalNotes = 0;
            HitCount = 0;
        }
    }
}
