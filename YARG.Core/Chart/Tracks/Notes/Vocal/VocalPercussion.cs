namespace YARG.Core.Chart.Vocal
{
    public struct VocalPercussion
    {
        public bool IsPlayable { get; set; }
        public void TogglePlayability() { IsPlayable = !IsPlayable; }
    }
}
