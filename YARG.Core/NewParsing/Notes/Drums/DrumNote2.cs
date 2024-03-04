using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumNote<TPads> : IInstrumentNote
         where TPads : unmanaged, IDrumPadConfig
    {
        public static readonly int NUM_PADS;
        static IDrumNote()
        {
            TPads d = default;
            NUM_PADS = d.NumPads;
        }

        public DualTime Bass { get; set; }
        public DualTime DoubleBass { get; set; }
        public ref TPads Pads { get; }
        public bool IsFlammed { get; set; } 
        public void ToggleDoubleBass();
        public void DisableBass();
        public DualTime this[int lane] { get; set; }

        /// <summary>
        /// Used for loading the buffer of unknown drum types to a known type
        /// </summary>
        /// <param name="unknown">Unknown drum type note</param>
        public void LoadFrom(in ProDrumNote2<FiveLane> unknown);
    }

    public struct DrumNote2<TPads> : IDrumNote<TPads>
        where TPads : unmanaged, IDrumPadConfig
    {
        private DualTime _bass;
        private DualTime _doubleBass;
        private TPads _pads;
        private bool _flammed;

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
                switch (lane)
                {
                    case 0: return _bass;
                    case 1: return _doubleBass;
                    default:
                        lane -= 2;
                        if (lane >= _pads.NumPads)
                        {
                            throw new ArgumentOutOfRangeException(nameof(lane));
                        }

                        unsafe
                        {
                            fixed (void* ptr = &_pads)
                            {
                                return ((DrumPad*) ptr)[lane - 2].Duration;
                            }
                        }
                }
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
                        lane -= 2;
                        if (lane >= _pads.NumPads)
                        {
                            throw new ArgumentOutOfRangeException(nameof(lane));
                        }

                        unsafe
                        {
                            fixed (void* ptr = &_pads)
                            {
                                ((DrumPad*) ptr)[lane - 2].Duration = value;
                            }
                        }
                        break;
                }
            }
        }

        public int GetNumActiveLanes()
        {
            int numActive = _bass.IsActive() || _doubleBass.IsActive() ? 1 : 0;
            unsafe
            {
                fixed (void* ptr = &_pads)
                {
                    var pads = (DrumPad*) ptr;
                    for (int i = 0; i < _pads.NumPads; ++i)
                    {
                        bool active = pads[i].IsActive();
                        numActive += Unsafe.As<bool, byte>(ref active);
                    }
                }
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

            unsafe
            {
                fixed (void* ptr = &_pads)
                {
                    var pads = (DrumPad*) ptr;
                    for (int i = 0; i < _pads.NumPads; ++i)
                    {
                        if (pads[i].Duration > sustain)
                        {
                            sustain = pads[i].Duration;
                        }
                    }
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

        /// <summary>
        /// Used for loading the buffer of unknown drum types to a known type
        /// </summary>
        /// <param name="unknown">Unknown drum type note</param>
        public void LoadFrom(in ProDrumNote2<FiveLane> unknown)
        {
            _bass = unknown.Bass;
            _doubleBass = unknown.DoubleBass;
            _flammed = unknown.IsFlammed;

            unsafe
            {
                fixed (void* ptr = &_pads)
                fixed (void* otherPads = &unknown.Pads)
                {
                    Unsafe.CopyBlock(ptr, otherPads, (uint) sizeof(TPads));
                }
            }
        }
    }

    public struct ProDrumNote2<TPads> : IDrumNote<TPads>
        where TPads : unmanaged, IDrumPadConfig
    {
        public unsafe struct CymbalArray
        {
            public const int NUM_CYMBALS = 3;
            private fixed bool values[NUM_CYMBALS];
            public ref bool this[int lane]
            {
                get
                {
                    if (lane < 0 || NUM_CYMBALS <= lane)
                    {
                        throw new ArgumentOutOfRangeException(nameof(lane));
                    }
                    return ref values[lane];
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
            if (Cymbals[0])
            {
                builder.Append($"Y-Cymbal | ");
            }
            if (Cymbals[1])
            {
                builder.Append($"B-Cymbal | ");
            }
            if (Cymbals[2])
            {
                builder.Append($"G-Cymbal | ");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Used for loading the buffer of unknown drum types to a known type
        /// </summary>
        /// <param name="unknown">Unknown drum type note</param>
        public void LoadFrom(in ProDrumNote2<FiveLane> unknown)
        {
            _baseDrumNote.LoadFrom(in unknown);
            unsafe
            {
                for (int i = 0; i < CymbalArray.NUM_CYMBALS; ++i)
                {
                    Cymbals[i] = unknown.Cymbals[i];
                }
            }
        }
    }
}
