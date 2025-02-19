using System;

namespace YARG.Core.NewParsing
{
    public unsafe struct ProKeyNote : IInstrumentNote
    {
        private static readonly PitchValidator VALIDATOR = new(3, 5);
        public PitchedKey Key1;
        public PitchedKey Key2;
        public PitchedKey Key3;
        public PitchedKey Key4;

        public readonly int GetNumActiveLanes()
        {
            int numActive = Key1.IsActive() ? 1 : 0;
            numActive += Key2.IsActive() ? 1 : 0;
            numActive += Key3.IsActive() ? 1 : 0;
            numActive += Key4.IsActive() ? 1 : 0;
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = DualTime.Zero;
            if (Key1.Pitch > 0 && Key1.Duration > sustain)
            {
                sustain = Key1.Duration;
            }
            if (Key2.Pitch > 0 && Key2.Duration > sustain)
            {
                sustain = Key2.Duration;
            }
            if (Key3.Pitch > 0 && Key3.Duration > sustain)
            {
                sustain = Key3.Duration;
            }
            if (Key4.Pitch > 0 && Key4.Duration > sustain)
            {
                sustain = Key4.Duration;
            }
            return sustain;
        }

        public bool Add(int binary, in DualTime length)
        {
            return binary != 0 && VALIDATOR.ValidateBinary(binary) && AddNote(binary, in length);
        }

        public bool Add(int octave, PitchName pitch, in DualTime length)
        {
            return VALIDATOR.ValidateOctaveAndPitch(octave, pitch, out int binary) && binary != 0 && AddNote(binary, in length);
        }

        public bool Remove(int pitch)
        {
            if (pitch == 0)
            {
                return false;
            }

            if (Key1.Pitch == pitch)
            {
                Key1.Pitch = 0;
                Key1.Duration = DualTime.Zero;
            }
            else if (Key2.Pitch == pitch)
            {
                Key2.Pitch = 0;
                Key2.Duration = DualTime.Zero;
            }
            else if (Key3.Pitch == pitch)
            {
                Key3.Pitch = 0;
                Key3.Duration = DualTime.Zero;
            }
            else if (Key4.Pitch == pitch)
            {
                Key4.Pitch = 0;
                Key4.Duration = DualTime.Zero;
            }
            else
            {
                return false;
            }
            return true;
        }

        public bool SetLength(int pitch, in DualTime length)
        {
            if (pitch == 0)
            {
                return false;
            }

            if (Key1.Pitch == pitch)
            {
                Key1.Duration = length;
            }
            else if (Key2.Pitch == pitch)
            {
                Key2.Duration = length;
            }
            else if (Key3.Pitch == pitch)
            {
                Key3.Duration = length;
            }
            else if (Key4.Pitch == pitch)
            {
                Key4.Duration = length;
            }
            else
            {
                return false;
            }
            return true;
        }

        public bool SetPitch(int pitchToFind, int pitchToSet)
        {
            if (!VALIDATOR.ValidateBinary(pitchToSet))
            {
                return false;
            }

            if (Key1.Pitch == pitchToFind)
            {
                Key1.Pitch = pitchToSet;
            }
            else if (Key2.Pitch == pitchToFind)
            {
                Key2.Pitch = pitchToSet;
            }
            else if (Key3.Pitch == pitchToFind)
            {
                Key3.Pitch = pitchToSet;
            }
            else if (Key4.Pitch == pitchToFind)
            {
                Key4.Pitch = pitchToSet;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool AddNote(int binary, in DualTime length)
        {
            if (Key1.Pitch == binary
            ||  Key2.Pitch == binary
            ||  Key3.Pitch == binary
            ||  Key4.Pitch == binary)
            {
                throw new Exception("Duplicate pitches are not allowed");
            }

            if (Key1.Pitch == 0)
            {
                Key1.Pitch = binary;
                Key1.Duration = length;
            }
            else if (Key2.Pitch == 0)
            {
                Key2.Pitch = binary;
                Key2.Duration = length;
            }

            else if (Key3.Pitch == 0)
            {
                Key3.Pitch = binary;
                Key3.Duration = length;
            }

            else if (Key4.Pitch == 0)
            {
                Key4.Pitch = binary;
                Key4.Duration = length;
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}
