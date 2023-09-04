using System;
namespace YARG.Core.Chart.Pitch
{
    public interface IPitchConfig
    {
        public int OCTAVE_MIN();
        public int OCTAVE_MAX();
    }

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

    public struct Pitch<TConfig>
        where TConfig : IPitchConfig, new()
    {
        public static readonly TConfig CONFIG;
        static Pitch() { CONFIG = new(); }

        public const byte OCTAVE_LENGTH = 12;

        private PitchName _note;
        private int _octave;
        private int _binary;

        public PitchName Note
        {
            get { return _note; }
            set
            {
                if (_octave == CONFIG.OCTAVE_MAX() && value != PitchName.C)
                    throw new Exception("Pitch out of range");

                int binaryNote = (int)value;
                _note = value;
                _binary = (_octave + 1) * OCTAVE_LENGTH + binaryNote;
            }
        }
        public int Octave
        {
            get { return _octave; }
            set
            {
                if (value < CONFIG.OCTAVE_MIN() || CONFIG.OCTAVE_MAX() < value || (value == CONFIG.OCTAVE_MAX() && _note != PitchName.C))
                    throw new Exception("Octave out of range");

                int binaryOctave = (value + 1) * OCTAVE_LENGTH;
                _octave = value;
                _binary = (int) _note + binaryOctave;
            }
        }
        public int Binary
        {
            get { return _binary; }
            set
            {
                int octave = value / OCTAVE_LENGTH - 1;
                var note = (PitchName) (value % OCTAVE_LENGTH);
                if (octave < CONFIG.OCTAVE_MIN() || CONFIG.OCTAVE_MAX() < octave || (octave == CONFIG.OCTAVE_MAX() && note != PitchName.C))
                    throw new Exception("Binary pitch value out of range");

                _binary = value;
                _octave = octave;
                _note = note;
            }
        }
    }
}
