using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public unsafe struct ProKeyNote : IInstrumentNote
    {
#pragma warning disable CS0169
        private PitchedKey _key1;
        private PitchedKey _key2;
        private PitchedKey _key3;
        private PitchedKey _key4;
#pragma warning restore CS0169
        private int _numActive;

        public ref PitchedKey this[int index]
        {
            get
            {
                if (index < 0 || index >= _numActive)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                fixed (PitchedKey* ptr = &_key1)
                {
                    return ref ptr[index];
                }
            }
        }

        public readonly int GetNumActiveLanes()
        {
            return _numActive;
        }

        public bool Add(int binary, in DualTime length)
        {
            if (_numActive == 4)
            {
                return false;
            }

            PitchedKey key = new()
            {
                Duration = length,
                Pitch = new() { Binary = binary },
            };
            AddNote(in key, binary);
            return true;
        }

        public bool Add(PitchName note, int octave, in DualTime length)
        {
            if (_numActive == 4)
            {
                return false;
            }

            PitchedKey key = new()
            {
                Duration = length,
                Pitch = new() { Note = note, Octave = octave },
            };
            AddNote(in key, key.Pitch.Binary);
            return true;
        }

        public bool Remove(int index)
        {
            if (index >= _numActive)
            {
                return false;
            }

            --_numActive;
            fixed (PitchedKey* keys = &_key1)
            {
                for (int i = index; i < _numActive; ++i)
                {
                    keys[i] = keys[i + 1];
                }
            }
            return true;
        }

        public bool SetLength(int index, in DualTime length)
        {
            if (index < 0 || index >= _numActive)
            {
                return false;
            }

            fixed (PitchedKey* ptr = &_key1)
            {
                ptr[index].Duration = length;
            }
            return true;
        }

        public bool SetPitch(int index, PitchName note)
        {
            if (index < 0 || index >= _numActive)
            {
                return false;
            }

            fixed (PitchedKey* ptr = &_key1)
            {
                ptr[index].Pitch.Note = note;
            }
            return true;
        }

        public bool SetOctave(int index, int octave)
        {
            if (index < 0 || index >= _numActive)
            {
                return false;
            }

            fixed (PitchedKey* ptr = &_key1)
            {
                ptr[index].Pitch.Octave = octave;
            }
            return true;
        }

        public bool SetBinary(int index, int binary)
        {
            if (index < 0 || index >= _numActive)
            {
                return false;
            }

            fixed (PitchedKey* ptr = &_key1)
            {
                ptr[index].Pitch.Binary = binary;
            }
            return true;
        }

        public readonly DualTime GetLongestSustain()
        {
            DualTime sustain = default;
            fixed (PitchedKey* keys = &_key1)
            {
                for (int i = 0; i < _numActive; ++i)
                {
                    var dur = keys[i].Duration;
                    if (dur > sustain)
                    {
                        sustain = dur;
                    }
                }
            }
            return sustain;
        }

        private void AddNote(in PitchedKey key, int binary)
        {
            uint i = 0;
            fixed (PitchedKey* keys = &_key1)
            {
                while (i < _numActive)
                {
                    int cmp = keys[i].Pitch.Binary;
                    if (cmp == binary)
                    {
                        throw new Exception("Duplicate pitches are not allowed");
                    }

                    if (cmp > binary)
                    {
                        for (int j = _numActive; j > i; --j)
                        {
                            keys[j] = keys[j - 1];
                        }
                        break;
                    }
                    ++i;
                }
                keys[i] = key;
            }
            _numActive++;
        }
    }
}
