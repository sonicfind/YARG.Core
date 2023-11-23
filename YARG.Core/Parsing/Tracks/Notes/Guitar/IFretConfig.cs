using System.Text;

namespace YARG.Core.Parsing.Guitar
{
    public unsafe interface IFretConfig { }

    public struct FiveFret : IFretConfig
    {
        public TruncatableSustain Open;
        public TruncatableSustain Green;
        public TruncatableSustain Red;
        public TruncatableSustain Yellow;
        public TruncatableSustain Blue;
        public TruncatableSustain Orange;

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Open.IsActive())
                stringBuilder.Append($"Open: {Open} | ");
            if (Green.IsActive())
                stringBuilder.Append($"Green: {Green} | ");
            if (Red.IsActive())
                stringBuilder.Append($"Red: {Red} | ");
            if (Yellow.IsActive())
                stringBuilder.Append($"Yellow: {Yellow} | ");
            if (Blue.IsActive())
                stringBuilder.Append($"Blue: {Blue} | ");
            if (Orange.IsActive())
                stringBuilder.Append($"Orange: {Orange}");
            return stringBuilder.ToString();
        }
    }

    public struct SixFret : IFretConfig
    {
        public TruncatableSustain Open;
        public TruncatableSustain Black1;
        public TruncatableSustain Black2;
        public TruncatableSustain Black3;
        public TruncatableSustain White1;
        public TruncatableSustain White2;
        public TruncatableSustain White3;

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Open.IsActive())
                stringBuilder.Append($"Open: {Open} | ");
            if (Black1.IsActive())
                stringBuilder.Append($"Black 1: {Black1} | ");
            if (Black2.IsActive())
                stringBuilder.Append($"Black 2: {Black2} | ");
            if (Black3.IsActive())
                stringBuilder.Append($"Black 3: {Black3} | ");
            if (White1.IsActive())
                stringBuilder.Append($"White 1: {White1} | ");
            if (White2.IsActive())
                stringBuilder.Append($"White 2: {White2} | ");
            if (White3.IsActive())
                stringBuilder.Append($"White 3: {White3}");
            return stringBuilder.ToString();
        }
    }
}
