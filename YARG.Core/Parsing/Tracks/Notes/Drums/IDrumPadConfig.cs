using System;
using System.Text;

namespace YARG.Core.Parsing.Drums
{
    public interface IDrumPadConfig
    {
        public ref DrumPad this[int index] { get; }

        protected static void WriteDynamics(StringBuilder builder, ref DrumPad pad)
        {
            if (pad.Dynamics != DrumDynamics.None)
            {
                builder.Append($"- {pad.Dynamics} ");
            }
            builder.Append("| ");
        }
    }

    public struct DrumPad_4 : IDrumPadConfig
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Green;

        public ref DrumPad this[int index]
        {
            get
            {
                if (0 <= index && index < 4)
                {
                    unsafe
                    {
                        fixed (DrumPad* pads = &Snare)
                            return ref pads[index];
                    }
                }
                throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Snare);
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Yellow);
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Blue);
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Green);
            }
            return builder.ToString();
        }
    }

    public struct DrumPad_5 : IDrumPadConfig
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Orange;
        public DrumPad Green;

        public ref DrumPad this[int index]
        {
            get
            {
                if (0 <= index && index < 5)
                {
                    unsafe
                    {
                        fixed (DrumPad* pads = &Snare)
                            return ref pads[index];
                    }
                }
                throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();

            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Snare);
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Yellow);
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Blue);
            }
            if (Orange.IsActive())
            {
                builder.Append($"Orange: {Orange.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Orange);
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green.Duration} ");
                IDrumPadConfig.WriteDynamics(builder, ref Green);
            }
            return builder.ToString();
        }
    }
}
