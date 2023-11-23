namespace YARG.Core.Parsing.ProGuitar
{
    public struct HandPosition<TFretConfig>
        where TFretConfig : IProFretConfig, new()
    {
        private static readonly TFretConfig CONFIG = new();
        private int _value;

        public static implicit operator int(HandPosition<TFretConfig> position) => position._value;
        public static implicit operator HandPosition<TFretConfig>(int position)
        {
            CONFIG.ThrowIfOutOfRange(position);
            return new HandPosition<TFretConfig>()
            {
                _value = position
            };
        }
    }
}
