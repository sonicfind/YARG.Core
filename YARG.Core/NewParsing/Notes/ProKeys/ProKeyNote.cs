using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public unsafe struct ProKeyNote : IInstrumentNote
    {
        private static readonly PitchValidator VALIDATOR = new(3, 5);
#pragma warning disable CS0649
        private PitchedKey _key1;
        private PitchedKey _key2;
        private PitchedKey _key3;
        private PitchedKey _key4;
#pragma warning restore CS0649
        private int _numActive;

        public readonly int NUMLANES => 25;

        public readonly int GetNumActiveLanes()
        {
            return _numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            DualTime sustain = default;
            switch (_numActive)
            {
                case 4: goto Key4;
                case 3: goto Key3;
                case 2: goto Key2;
                case 1: goto Key1;
                default: goto End;
            }
        Key4:
            sustain = _key4.Duration;
        Key3:
            if (_key3.Duration > sustain)
            {
                sustain = _key3.Duration;
            }
        Key2:
            if (_key2.Duration > sustain)
            {
                sustain = _key2.Duration;
            }
        Key1:
            if (_key1.Duration > sustain)
            {
                sustain = _key1.Duration;
            }
        End:
            return sustain;
        }

        public static bool Add(ProKeyNote* note, int binary, in DualTime length)
        {
            if (note->_numActive == 4 || !VALIDATOR.ValidateBinary(binary))
            {
                return false;
            }
            AddNote(note, binary, in length);
            return true;
        }

        public static bool Add(ProKeyNote* note, int octave, PitchName pitch, in DualTime length)
        {
            if (note->_numActive == 4 || !VALIDATOR.ValidateOctaveAndPitch(octave, pitch, out int binary))
            {
                return false;
            }
            AddNote(note, binary, in length);
            return true;
        }

        public static bool Remove(ProKeyNote* note, int pitch)
        {
            if (pitch > 0)
            {
                var keys = &note->_key1;
                for (int i = 0; i < 4; ++i)
                {
                    if (keys[i].Pitch == pitch)
                    {
                        keys[i].Pitch = 0;
                        --note->_numActive;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool SetLength(ProKeyNote* note, int pitch, in DualTime length)
        {
            if (pitch > 0)
            {
                var keys = &note->_key1;
                for (int i = 0; i < 4; ++i)
                {
                    if (keys[i].Pitch == pitch)
                    {
                        keys[i].Duration = length;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool SetPitch(ProKeyNote* note, int pitchToFind, int pitchToSet)
        {
            if (note->_numActive > 0 && VALIDATOR.ValidateBinary(pitchToSet))
            {
                var keys = &note->_key1;
                for (int i = 0; i < 4; ++i)
                {
                    if (keys[i].Pitch == pitchToFind)
                    {
                        keys[i].Pitch = pitchToSet;
                        if (pitchToSet == 0)
                        {
                            --note->_numActive;
                        }
                        return true;
                    }
                }
            }
            return true;
        }

        private static void AddNote(ProKeyNote* note, int binary, in DualTime length)
        {
            var keys = &note->_key1;
            int index = -1;
            for (int i = 0; i < 4; ++i)
            {
                if (keys[i].Pitch == binary)
                {
                    throw new Exception("Duplicate pitches are not allowed");
                }
                if (index == -1 && keys[i].Pitch == 0)
                {
                    index = i;
                }
            }
            keys[index].Pitch = binary;
            keys[index].Duration = length;
            note->_numActive++;
        }
    }
}
