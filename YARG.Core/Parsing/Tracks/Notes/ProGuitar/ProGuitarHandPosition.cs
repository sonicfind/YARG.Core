namespace YARG.Core.Parsing.ProGuitar
{
    public struct HandPosition<TFretConfig>
        where TFretConfig : IProFretConfig, new()
    {
        private static readonly TFretConfig CONFIG = new();
        private int fret;

        private HandPosition(int position)
        {
            CONFIG.ThrowIfOutOfRange(position);
            fret = position;
        }

        public static implicit operator int(HandPosition<TFretConfig> position) => position.fret;
        public static implicit operator HandPosition<TFretConfig>(int position) => new(position);
    }
}
