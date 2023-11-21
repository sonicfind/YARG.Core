namespace YARG.Core.Parsing
{
    public interface IEnableable
    {
        public bool IsActive();
        public void Disable();
    }
}
