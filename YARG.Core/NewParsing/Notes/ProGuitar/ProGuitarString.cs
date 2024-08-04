using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum StringMode
    {
        Normal,
        Ghost,
        Bend,
        Muted,
        Tapped,
        Harmonics,
        Pinch_Harmonics
    };

    public struct ProGuitarString<TProFret>
        where TProFret : unmanaged, IProFret
    {
        public TProFret Fret;
        public StringMode Mode;
        public DualTime Duration;

        public readonly bool IsActive()
        {
            return Duration.IsActive();
        }

        public void Disable()
        {
            Mode = StringMode.Normal;
            Duration = default;
        }

        public override readonly string ToString()
        {
            var builder = new StringBuilder($"{Fret.Value} - {Duration.Ticks}");
            if (Mode != StringMode.Normal)
            {
                builder.Append($"({Mode})");
            }
            return builder.ToString();
        }
    }
}
