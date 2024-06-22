using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumNote<TPads> : IInstrumentNote
         where TPads : unmanaged, IDrumPadConfig<TPads>
    {
        public DualTime Bass { get; set; }
        public DualTime DoubleBass { get; set; }
        public ref TPads Pads { get; }
        public bool IsFlammed { get; set; } 
        public void ToggleDoubleBass();
        public void DisableBass();
        public DualTime this[int lane] { get; set; }
    }

    public struct DrumNote2<TPads> : IDrumNote<TPads>
        where TPads : unmanaged, IDrumPadConfig<TPads>
    {
        private DualTime _bass;
        private DualTime _doubleBass;
        private bool _flammed;
        private TPads _pads;

        public DualTime Bass
        {
            readonly get { return _bass; }
            set
            {
                _bass = value;
                _doubleBass = default;
            }
        }

        public DualTime DoubleBass
        {
            readonly get { return _doubleBass; }
            set
            {
                _doubleBass = value;
                _bass = default;
            }
        }

        public ref TPads Pads
        {
            get
            {
                unsafe
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    return ref _pads;
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }
        }

        public bool IsFlammed
        {
            readonly get => _flammed;
            set => _flammed = value;
        }

        public void ToggleDoubleBass()
        {
            if (_bass.IsActive())
            {
                _doubleBass = _bass;
                _bass = default;
            }
            else if (_doubleBass.IsActive())
            {
                _bass = _doubleBass;
                _doubleBass = default;
            }
        }

        public void DisableBass()
        {
            _bass = default;
            _doubleBass = default;
        }

        public DualTime this[int lane]
        {
            readonly get
            {
                return lane switch
                {
                    0 => _bass,
                    1 => _doubleBass,
                    _ => _pads[lane - 2],
                };
            }
            set
            {
                switch (lane)
                {
                    case 0:
                        _bass = value;
                        _doubleBass = default;
                        break;
                    case 1:
                        _doubleBass = value;
                        _bass = default;
                        break;
                    default:
                        _pads[lane - 2] = value;
                        break;
                }
            }
        }

        public int GetNumActiveLanes()
        {
            int numActive = _bass.IsActive() || _doubleBass.IsActive() ? 1 : 0;
            for (int i = 0; i < _pads.NumPads; ++i)
            {
                bool active = _pads[i].IsActive();
                numActive += Unsafe.As<bool, byte>(ref active);
            }
            return numActive;
        }

        public DualTime GetLongestSustain()
        {
            var sustain = _bass;
            if (_doubleBass.IsActive())
            {
                sustain = _doubleBass;
            }

            for (int i = 0; i < _pads.NumPads; ++i)
            {
                if (_pads[i] > sustain)
                {
                    sustain = _pads[i];
                }
            }
            return sustain;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (_bass.IsActive())
            {
                stringBuilder.Append($"Bass: {_bass.Ticks} | ");
            }
            else if (_doubleBass.IsActive())
            {
                stringBuilder.Append($"DoubleBass: {_doubleBass.Ticks} | ");
            }
            stringBuilder.Append(Pads.ToString());
            return stringBuilder.ToString();
        }
    }

    public unsafe struct ProDrumNote2<TPads> : IDrumNote<TPads>
        where TPads : unmanaged, IDrumPadConfig<TPads>
    {
        public struct CymbalArray
        {
            public bool Yellow;
            public bool Blue;
            public bool Green;

            public ref bool this[int lane]
            {
                get
                {
                    switch (lane)
                    {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                        case 0: return ref Yellow;
                        case 1: return ref Blue;
                        case 2: return ref Green;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(lane));
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                    }
                }
            }
        }

        private DrumNote2<TPads> _baseDrumNote;
        public CymbalArray Cymbals;

        public DualTime Bass
        {
            readonly get => _baseDrumNote.Bass;
            set => _baseDrumNote.Bass = value;
        }

        public DualTime DoubleBass
        {
            readonly get => _baseDrumNote.DoubleBass;
            set => _baseDrumNote.DoubleBass = value;
        }

        public ref TPads Pads => ref _baseDrumNote.Pads;

        public bool IsFlammed
        {
            readonly get => _baseDrumNote.IsFlammed;
            set => _baseDrumNote.IsFlammed = value;
        }

        public void ToggleDoubleBass() => _baseDrumNote.ToggleDoubleBass();

        public void DisableBass() => _baseDrumNote.DisableBass();

        public DualTime this[int lane]
        {
            readonly get => _baseDrumNote[lane];
            set => _baseDrumNote[lane] = value;
        }

        public int GetNumActiveLanes() => _baseDrumNote.GetNumActiveLanes();

        public DualTime GetLongestSustain() => _baseDrumNote.GetLongestSustain();

        public override string ToString()
        {
            StringBuilder builder = new(_baseDrumNote.ToString());
            if (Cymbals.Yellow)
            {
                builder.Append($"Y-Cymbal | ");
            }
            if (Cymbals.Blue)
            {
                builder.Append($"B-Cymbal | ");
            }
            if (Cymbals.Green)
            {
                builder.Append($"G-Cymbal | ");
            }
            return builder.ToString();
        }
    }
}
