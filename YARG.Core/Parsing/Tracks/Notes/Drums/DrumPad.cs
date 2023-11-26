namespace YARG.Core.Parsing.Drums
{
    public enum DrumDynamics
    {
        None,
        Accent,
        Ghost
    }

    public struct DrumPad : IEnableable
    {
        public DualTime Duration;

        public DrumDynamics Dynamics;

        public bool IsActive()
        {
            return Duration.IsActive();
        }
        public void Disable()
        {
            Duration.Disable();
            Dynamics = DrumDynamics.None;
        }
    }
}
