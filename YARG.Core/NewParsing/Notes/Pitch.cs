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

    /// <summary>
    /// Used for checking whether certain pitch values are within valid ranges
    /// </summary>
    public readonly struct PitchValidator
    {
        public const int OCTAVE_LENGTH = 12;
        public readonly int OCTAVE_MIN;
        public readonly int OCTAVE_MAX;

        /// <summary>
        /// Creates a validator with a specific octave range
        /// </summary>
        /// <param name="min">The minimum octave</param>
        /// <param name="max">The maximum octave (exclusive aside from <see cref="PitchName.C"/>)</param>
        public PitchValidator(int min, int max)
        {
            OCTAVE_MIN = min;
            OCTAVE_MAX = max;
        }

        /// <summary>
        /// Returns whether the binary value rests within range
        /// </summary>
        /// <param name="binary">The binary value</param>
        public bool ValidateBinary(int binary)
        {
            int octave = binary / OCTAVE_LENGTH - 1;
            return binary == 0 || (OCTAVE_MIN <= octave && octave <= OCTAVE_MAX && (octave != OCTAVE_MAX || binary % OCTAVE_LENGTH == (int)PitchName.C));
        }

        /// <summary>
        /// Returns whether the octave and pitch combination reside within range.<br></br>
        /// The resulting binary value is passed back out as well.
        /// </summary>
        /// <param name="octave">The octave</param>
        /// <param name="pitch">The key within the octave</param>
        /// <param name="binary">The resulting binary value of the octave+pitch combo</param>
        /// <returns>Whether the combo is valid</returns>
        public bool ValidateOctaveAndPitch(int octave, PitchName pitch, out int binary)
        {
            binary = (octave + 1) * OCTAVE_LENGTH + (int) pitch;
            return binary == 0 || (OCTAVE_MIN <= octave && octave <= OCTAVE_MAX && (octave != OCTAVE_MAX || pitch == PitchName.C));
        }
    }
}
