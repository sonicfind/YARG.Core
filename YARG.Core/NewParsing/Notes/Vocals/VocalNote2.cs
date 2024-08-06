using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum TalkieState
    {
        None,
        Talkie,
        Lenient
    }

    public struct VocalNote2
    {
        public static readonly PitchValidator VALIDATOR = new(2, 6);
        private int _pitch;

        public string Lyric;
        public DualTime Duration;
        public TalkieState TalkieState;

        public int Pitch
        {
            readonly get => _pitch;
            set
            {
                if (!SetPitch(value))
                {
                    throw new ArgumentException("pitch");
                }
            }
        }

        public bool SetPitch(int note)
        {
            if (!VALIDATOR.ValidateBinary(note))
            {
                return false;
            }
            _pitch = note;
            return true;
        }

        public bool SetPitch(int octave, PitchName key)
        {
            if (!VALIDATOR.ValidateOctaveAndPitch(octave, key, out int pitch))
            {
                return false;
            }
            _pitch = pitch;
            return true;
        }

        public readonly bool IsPlayable() { return Duration.Ticks > 0 && (_pitch >= 0 || TalkieState != TalkieState.None); }
    }
}
