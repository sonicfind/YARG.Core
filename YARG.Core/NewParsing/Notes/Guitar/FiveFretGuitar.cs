using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct FiveFretGuitar : IGuitarNote
    {
        public DualTime Open;
        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        private GuitarState _state;

        public GuitarState State
        {
            readonly get => _state;
            set => _state = value;
        }

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Open.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Green.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Red.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Orange.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Open;
            if (Green > sustain)
            {
                sustain = Green;
            }
            if (Red > sustain)
            {
                sustain = Red;
            }
            if (Yellow > sustain)
            {
                sustain = Yellow;
            }
            if (Blue > sustain)
            {
                sustain = Blue;
            }
            if (Orange > sustain)
            {
                sustain = Orange;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Open.IsActive())
            {
                stringBuilder.Append($"Open: {Open.Ticks} | ");
            }
            if (Green.IsActive())
            {
                stringBuilder.Append($"Green: {Green.Ticks} | ");
            }
            if (Red.IsActive())
            {
                stringBuilder.Append($"Red: {Red.Ticks} | ");
            }
            if (Yellow.IsActive())
            {
                stringBuilder.Append($"Yellow: {Yellow.Ticks} | ");
            }
            if (Blue.IsActive())
            {
                stringBuilder.Append($"Blue: {Blue.Ticks} | ");
            }
            if (Orange.IsActive())
            {
                stringBuilder.Append($"Orange: {Orange.Ticks}");
            }
            if (_state != GuitarState.Natural)
            {
                stringBuilder.Append(_state.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}
