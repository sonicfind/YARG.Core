using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct HandPosition<TProFretConfig>
        where TProFretConfig : unmanaged, IProFretConfig<TProFretConfig>
    {
        private int _fret;

        public int Fret
        {
            readonly get => _fret;
            set
            {
                _fret = IProFretConfig<TProFretConfig>.ValidateFret(value);
            }
        }
    }
}
