namespace YARG.Core.Chart
{
    public struct VocalPercussion
    {
        public bool IsPlayable { get; set; }
        public void TogglePlayability() { IsPlayable = !IsPlayable; }
    }
}
