namespace YARG.Core.Chart.Vocal
{
    public struct VocalPercussion
    {
        public bool IsPlayable;
        public void TogglePlayability() { IsPlayable = !IsPlayable; }
    }
}
