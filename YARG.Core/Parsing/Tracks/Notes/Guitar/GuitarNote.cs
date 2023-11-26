using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.Parsing.Guitar
{
    public enum GuitarState
    {
        NATURAL,
        FORCED_LEGACY,
        HOPO,
        STRUM,
        TAP
    }

    public static class GuitarSettings
    {

    }

    public struct GuitarNote<TConfig> : INote, IDotChartLoadable
        where TConfig : unmanaged, IFretConfig
    {
        public static readonly int NUMLANES;

        static GuitarNote()
        {
            unsafe
            {
                NUMLANES = sizeof(TConfig) / sizeof(DualTime);
            }
        }

        private TConfig frets;
        public GuitarState State;

        private ref DualTime GetSustain(int lane)
        {
            if (lane < 0 || NUMLANES <= lane)
                throw new IndexOutOfRangeException();

            unsafe
            {
                fixed (TConfig* ptr = &frets)
                    return ref ((DualTime*) ptr)[lane];
            }
        }

        public DualTime this[int lane]
        {
            get => GetSustain(lane);
            set
            {
                if (lane < 0 || NUMLANES <= lane)
                    throw new IndexOutOfRangeException();

                SetValues(lane, value);
            }
        }

        public bool SetFromDotChart(int lane, in DualTime length)
        {
            var selection = frets.ParseLane(lane);
            if (selection == IFretConfig.LaneSelection.None)
                return false;

            if (selection <= IFretConfig.LaneSelection.Lane_6)
                SetValues(selection - IFretConfig.LaneSelection.Open, length);
            else if (selection == IFretConfig.LaneSelection.Tap)
                State = GuitarState.TAP;
            else if (State == GuitarState.NATURAL)
                State = GuitarState.FORCED_LEGACY;
            return true;
        }

        public void Disable(int lane)
        {
            GetSustain(lane).Disable();
        }

        public int GetNumActiveNotes()
        {
            int numActive = 0;
            unsafe
            {
                fixed (TConfig* ptr = &frets)
                {
                    var lanes = (DualTime*) ptr;
                    for (int i = 0; i < NUMLANES; ++i)
                    {
                        bool active = lanes[i].IsActive();
                        numActive += Unsafe.As<bool, byte>(ref active);
                    }
                }
            }
            return numActive;
        }

        public DualTime GetLongestSustain()
        {
            unsafe
            {
                fixed (TConfig* ptr = &frets)
                {
                    var lanes = (DualTime*) ptr;
                    var sustain = lanes[0];
                    for (int i = 1; i < NUMLANES; ++i)
                    {
                        var dur = lanes[i];
                        if (dur > sustain)
                            sustain = dur;
                    }
                    return sustain;
                }
            }
        }

        private void SetValues(int lane, in DualTime length)
        {
            unsafe
            {
                fixed (TConfig* ptr = &frets)
                {
                    var lanes = (DualTime*) ptr;
                    lanes[lane] = DualTime.Truncate(length);
                    if (lane == 0)
                    {
                        for (int i = 1; i < NUMLANES; ++i)
                            lanes[lane].Disable();
                    }
                    else
                        lanes[0].Disable();
                }
            }
        }
    }
}
