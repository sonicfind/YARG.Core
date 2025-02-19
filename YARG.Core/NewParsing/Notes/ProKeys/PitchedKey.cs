namespace YARG.Core.NewParsing
{
    public struct PitchedKey
    {
        public int Pitch;
        public DualTime Duration;

        public readonly bool IsActive()
        {
            return Pitch > 0 && Duration.IsActive();
        }
    }
}
