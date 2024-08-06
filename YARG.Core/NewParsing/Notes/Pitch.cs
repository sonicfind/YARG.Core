using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing
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

    public readonly struct PitchValidator
    {
        public const int OCTAVE_LENGTH = 12;
        public readonly int OCTAVE_MIN;
        public readonly int OCTAVE_MAX;

        public PitchValidator(int min, int max)
        {
            OCTAVE_MIN = min;
            OCTAVE_MAX = max;
        }

        public bool ValidateBinary(int binary)
        {
            int octave = binary / OCTAVE_LENGTH - 1;
            return binary == 0 || (OCTAVE_MIN <= octave && octave <= OCTAVE_MAX && (octave != OCTAVE_MAX || binary % OCTAVE_LENGTH == (int)PitchName.C));
        }

        public bool ValidateOctaveAndPitch(int octave, PitchName pitch, out int binary)
        {
            binary = (octave + 1) * OCTAVE_LENGTH + (int) pitch;
            return binary == 0 || (OCTAVE_MIN <= octave && octave <= OCTAVE_MAX && (octave != OCTAVE_MAX || pitch == PitchName.C));
        }
    }
}
