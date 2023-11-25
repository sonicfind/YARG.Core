using System.Text;
using static YARG.Core.Parsing.Guitar.IFretConfig;

namespace YARG.Core.Parsing.Guitar
{
    public interface IFretConfig
    {
        protected const int FORCED_VALUE = 5;
        protected const int TAPPED_VALUE = 6;
        protected const int OPENNOTE = 7;
        protected const int BASE_RANGE = 5;
        protected const int OPEN_INDEX = 0;

        public enum LaneSelection
        {
            Open,
            Lane_1,
            Lane_2,
            Lane_3,
            Lane_4,
            Lane_5,
            Lane_6,
            Forced,
            Tap,
            None,
        }

        public LaneSelection ParseLane(int lane);
    }

    public struct FiveFret : IFretConfig
    {
        public TruncatableSustain Open;
        public TruncatableSustain Green;
        public TruncatableSustain Red;
        public TruncatableSustain Yellow;
        public TruncatableSustain Blue;
        public TruncatableSustain Orange;

        public LaneSelection ParseLane(int lane)
        {
            return lane switch
            {
                < BASE_RANGE => LaneSelection.Lane_1 + lane,
                OPENNOTE     => LaneSelection.Open,
                FORCED_VALUE => LaneSelection.Forced,
                TAPPED_VALUE => LaneSelection.Tap,
                _ => LaneSelection.None,
            };
        }

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
        private static readonly int[] SIXFRETLANES = new int[5] { 3, 4, 5, 0, 1 };
        private const int BLACK_LANE3 = 8;

        public TruncatableSustain Open;
        public TruncatableSustain Black1;
        public TruncatableSustain Black2;
        public TruncatableSustain Black3;
        public TruncatableSustain White1;
        public TruncatableSustain White2;
        public TruncatableSustain White3;

        public LaneSelection ParseLane(int lane)
        {
            return lane switch
            {
                < BASE_RANGE => LaneSelection.Lane_1 + SIXFRETLANES[lane],
                BLACK_LANE3  => LaneSelection.Lane_3,
                OPENNOTE     => LaneSelection.Open,
                FORCED_VALUE => LaneSelection.Forced,
                TAPPED_VALUE => LaneSelection.Tap,
                _ => LaneSelection.None,
            };
        }

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
