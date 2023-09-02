using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.Chart
{
    public enum PitchName
    {
        C,
        C_Sharp_Db,
        D,
        D_Sharp_Eb,
        E,
        F,
        F_Sharp_Gb,
        G,
        G_Sharp_Ab,
        A,
        A_Sharp_Bb,
        B,
    };

    public interface IPitched
    {
        protected const byte OCTAVE_LENGTH = 12;
        public int OCTAVE_MIN { get; }
        public int OCTAVE_MAX { get; }

        public PitchName Note { get; set; }
        public int Octave { get; set; }
        public int Binary { get; set; }

        public static int ThrowIfInvalidPitch(IPitched pitched, PitchName pitch)
        {
            if (pitched.Octave == pitched.OCTAVE_MAX && pitch != PitchName.C)
                throw new Exception("Pitch out of range");
            return (int) pitch;
        }

        public static int ThrowIfInvalidOctave(IPitched pitched, int octave)
        {
            if (octave < pitched.OCTAVE_MIN || pitched.OCTAVE_MAX < octave || (octave == pitched.OCTAVE_MAX && pitched.Note != PitchName.C))
                throw new Exception("Octave out of range");
            return (octave + 1) * OCTAVE_LENGTH;
        }

        public static (int, PitchName) SplitBinary(IPitched pitched, int binary)
        {
            int octave = binary / OCTAVE_LENGTH - 1;
            var note = (PitchName) (binary % OCTAVE_LENGTH);
            if (octave < pitched.OCTAVE_MIN || pitched.OCTAVE_MAX < octave || (octave == pitched.OCTAVE_MAX && note != PitchName.C))
                throw new Exception("Binary pitch value out of range");
            return (octave, note);
        }
    }
}
