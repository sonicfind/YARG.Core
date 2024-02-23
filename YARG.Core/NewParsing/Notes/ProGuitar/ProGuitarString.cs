using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum StringMode
    {
        Normal,
        Bend,
        Muted,
        Tapped,
        Harmonics,
        Pinch_Harmonics
    };

    public struct ProGuitarString<TFretConfig>
        where TFretConfig : unmanaged, IProFretConfig
    {
        public static readonly TFretConfig CONFIG = default;

        private int _fret;
        public StringMode Mode;
        public DualTime Duration;

        public int Fret
        {
            readonly get => _fret;
            set
            {
                _fret = CONFIG.ValidateFret(value);
            }
        }

        public readonly bool IsActive()
        {
            return Duration.IsActive() && _fret >= 0;
        }

        public void Disable()
        {
            _fret = -1;
            Mode = StringMode.Normal;
            Duration = default;
            
        }
    }
}
