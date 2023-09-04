namespace YARG.Core.Chart
{
    public interface IEnableable
    {
        public bool IsActive();
        public void Disable();
    }
}
