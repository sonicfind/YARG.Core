using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct HandPosition<TFretConfig>
        where TFretConfig : unmanaged, IProFretConfig
    {
        private static readonly TFretConfig CONFIG = default;
        private int _fret;

        public int Fret
        {
            readonly get => _fret;
            set
            {
                _fret = CONFIG.ValidateFret(value);
            }
        }
    }
}
