namespace YARG.Core.Chart
{
    public interface IEnableable
    {
        public long Duration { get; set; }
        public bool IsActive();
        public void Disable();
    }
}
