using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public enum HitStatus
    {
        Idle,
        Hit,
        Sustained,
        Dropped,
        Missed
    }

    public struct BasicNote
    {
        public readonly DualTime EndTime;
        private         int _laneMask;
        private         HitStatus _status;

        public int LaneMask => _laneMask;

        public BasicNote(in DualTime endTime)
        {
            EndTime = endTime;
            _laneMask = 0;
            _status = HitStatus.Idle;
        }

        public void AddLane(int lane)
        {
            _laneMask |= 1 << lane;
        }

        public HitStatus Update()
        {
            return _status;
        }

        public void Reset()
        {
            _status = HitStatus.Idle;
        }
    }

    public abstract class YargPlayer
    {
    }
}
