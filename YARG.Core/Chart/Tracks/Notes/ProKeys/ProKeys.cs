using System;
using YARG.Core.Chart.Pitch;

namespace YARG.Core.Chart.ProKeys
{
    public unsafe struct ProKeyNote : INote
    {
        public struct Pitched_Key
        {
            public struct ProKeyConfig : IPitchConfig
            {
                public int OCTAVE_MIN() => 3;
                public int OCTAVE_MAX() => 5;
            }

            public Pitch<ProKeyConfig> Pitch;
            public TruncatableSustain Duration;

            public int OCTAVE_MIN => 3;
            public int OCTAVE_MAX => 5;

            public Pitched_Key(long length)
            {
                Pitch = default;
                Duration = length;
            }

            public Pitched_Key(long length, int binary) : this(length)
            {
                Pitch.Binary = binary;
            }

            public Pitched_Key(long length, PitchName note, int octave) : this(length)
            {
                Pitch.Note = note;
                Pitch.Octave = octave;
            }
        }

#pragma warning disable CS0169
        private Pitched_Key key_1;
        private Pitched_Key key_2;
        private Pitched_Key key_3;
        private Pitched_Key key_4;
#pragma warning restore CS0169
        private int _numActive;
        public int NumActive => _numActive;

        public Pitched_Key this[int index] => GetKey(index);

        private unsafe Pitched_Key* KeyPtr
        {
            get
            {
                fixed (Pitched_Key* keys = &key_1)
                    return keys;
            }
        }

        private ref Pitched_Key GetKey(int index)
        {
            if (index < _numActive)
            {
                switch (index)
                {
#pragma warning disable CS9084
                    case 0: return ref key_1;
                    case 1: return ref key_2;
                    case 2: return ref key_3;
                    case 3: return ref key_4;
#pragma warning restore CS9084
                }
            }
            throw new IndexOutOfRangeException();
        }

        public bool Add(int binary, long length)
        {
            if (_numActive == 4)
                return false;

            Pitched_Key key = new(length, binary);
            AddNote(ref key, binary);
            return true;
        }

        public bool Add(PitchName note, int octave, long length)
        {
            if (_numActive == 4)
                return false;

            Pitched_Key key = new(length, note, octave);
            AddNote(ref key, key.Pitch.Binary);
            return true;
        }

        public void Remove(int index)
        {
            if (index < _numActive)
            {
                --_numActive;
                unsafe
                {
                    fixed (Pitched_Key* keys = &key_1)
                    {
                        for (int i = index; i < _numActive; ++i)
                            keys[i] = keys[i + 1];
                    }
                }
            }
        }

        public void SetLength(int index, long length)
        {
            GetKey(index).Duration = length;
        }

        public void SetPitch(int index, PitchName note)
        {
            GetKey(index).Pitch.Note = note;
        }

        public void SetOctave(int index, int octave)
        {
            GetKey(index).Pitch.Octave = octave;
        }
        public void SetBinary(int index, int binary)
        {
            GetKey(index).Pitch.Binary = binary;
        }

        private void AddNote(ref Pitched_Key key, int binary)
        {
            uint i = 0;
            unsafe
            {
                fixed (Pitched_Key* keys = &key_1)
                {
                    while (i < _numActive)
                    {
                        int cmp = keys[i].Pitch.Binary;
                        if (cmp == binary)
                            throw new Exception("Duplicate pitches are not allowed");

                        if (cmp > binary)
                        {
                            for (int j = _numActive; j > i; --j)
                                keys[j] = keys[j - 1];
                            break;
                        }
                        ++i;
                    }
                    keys[i] = key;
                }
            }
            _numActive++;
        }

        public int GetNumActiveNotes()
        {
            return NumActive;
        }

        public long GetLongestSustain()
        {
            long sustain = 0;
            unsafe
            {
                fixed (Pitched_Key* keys = &key_1)
                {
                    for (int i = 0; i < _numActive; ++i)
                    {
                        long dur = keys[i].Duration;
                        if (dur > sustain)
                            sustain = dur;
                    }
                }
            }
            return sustain;
        }
    }
}
