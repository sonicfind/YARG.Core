﻿using System;
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

    public struct ProGuitarString<TProFretConfig>
        where TProFretConfig : unmanaged, IProFretConfig<TProFretConfig>
    {
        private int _fret;
        public StringMode Mode;
        public DualTime Duration;

        public int Fret
        {
            readonly get => _fret;
            set
            {
                _fret = IProFretConfig<TProFretConfig>.ValidateFret(value);
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

        public override readonly string ToString()
        {
            var builder = new StringBuilder($"{_fret} - {Duration.Ticks}");
            if (Mode != StringMode.Normal)
            {
                builder.Append($"({Mode})");
            }
            return builder.ToString();
        }
    }
}
