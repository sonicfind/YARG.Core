using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.Chart.Drums
{
    public struct DrumNote<TPads, TCymbals> : INote
        where TPads : unmanaged, IDrumPadConfig
        where TCymbals : unmanaged, ICymbalConfig
    {
        public static readonly int NUMPADS;
        public static readonly int NUMCYMBALS;
        static DrumNote()
        {
            unsafe
            {
                NUMPADS = sizeof(TPads) / sizeof(DrumPad);
                NUMCYMBALS = sizeof(TCymbals);
                if (NUMCYMBALS == 1)
                    NUMCYMBALS = 0;
            }
        }

        private TruncatableSustain _bass;
        private TruncatableSustain _doubleBass;
        public TPads Pads;
        public TCymbals Cymbals;

        public long Bass
        {
            get { return _bass; }
            set
            {
                _bass = value;
                _doubleBass.Disable();
            }
        }
        public long DoubleBass
        {
            get { return _doubleBass; }
            set
            {
                _doubleBass = value;
                _bass.Disable();
            }
        }

        public void ToggleDoubleBass()
        {
            if (_bass.IsActive())
            {
                _doubleBass = _bass;
                _bass.Disable();
            }
            else if (_doubleBass.IsActive())
            {
                _bass = _doubleBass;
                _doubleBass.Disable();
            }
        }

        public bool IsFlammed { get; set; }

        private unsafe DrumPad* PadPtr
        {
            get
            {
                fixed (TPads* pads = &Pads)
                    return (DrumPad*) pads;
            }
        }

        public long this[int lane]
        {
            get
            {
                if (lane == 0)
                    return _bass;
                if (lane == 1)
                    return _doubleBass;
                return Pads[lane - 2].Duration;
            }
            set
            {
                if (lane == 0)
                {
                    _bass = value;
                    _doubleBass.Disable();
                }
                else if (lane == 1)
                {
                    _doubleBass = value;
                    _bass.Disable();
                }
                else
                    Pads[lane - 2].Duration = value;
            }
        }

        public void DisableBass()
        {
            _bass.Disable();
            _doubleBass.Disable();
        }

        public int GetNumActiveNotes()
        {
            int numActive = _bass.IsActive() || _doubleBass.IsActive() ? 1 : 0;
            unsafe
            {
                var pads = PadPtr;
                for (int i = 0; i < NUMPADS; ++i)
                {
                    bool active = pads[i].IsActive();
                    numActive += Unsafe.As<bool, byte>(ref active);
                }
            }
            return numActive;
        }

        public long GetLongestSustain()
        {
            long sustain = _bass.Duration;
            if (_doubleBass.IsActive())
                sustain = _doubleBass.Duration;

            unsafe
            {
                var lanes = PadPtr;
                for (int i = 0; i < NUMPADS; ++i)
                {
                    long dur = lanes[i].Duration;
                    if (dur > sustain)
                        sustain = dur;
                }
            }
            return sustain;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (_bass.IsActive())
                stringBuilder.Append($"Bass: {_bass.Duration} | ");
            else if (_doubleBass.IsActive())
                stringBuilder.Append($"DoubleBass: {_doubleBass.Duration} | ");
            stringBuilder.Append(Pads.ToString());
            stringBuilder.Append(Cymbals.ToString());
            return stringBuilder.ToString();
        }

        public static DrumNote<TPads, TCymbals> Convert<TBasePadConfig>(ref DrumNote<TBasePadConfig, Pro_Drums> other)
            where TBasePadConfig : unmanaged, IDrumPadConfig
        {
            if (NUMPADS > DrumNote<TBasePadConfig, Pro_Drums>.NUMPADS)
                throw new InvalidOperationException("Not allowed to convert RAW chart representation from 4-lane to 5-lane");

            DrumNote<TPads, TCymbals> note = new()
            {
                _bass = other._bass,
                _doubleBass = other._doubleBass,
                IsFlammed = other.IsFlammed,
            };

            unsafe
            {
                fixed (TBasePadConfig* otherPads = &other.Pads)
                {
                    Unsafe.CopyBlock(&note.Pads, otherPads, (uint) (sizeof(DrumPad) * NUMPADS));
                }


                if (NUMCYMBALS == 3)
                {
                    fixed (Pro_Drums* otherCymbals = &other.Cymbals)
                    {
                        Unsafe.CopyBlock(&note.Cymbals, otherCymbals, (sizeof(bool) * 3));
                    }
                }
            }
            return note;
        }
    }
}
