namespace YARG.Core.Parsing.Vocal
{
    public struct VocalPercussion
    {
        public bool IsPlayable;
        public void TogglePlayability() { IsPlayable = !IsPlayable; }
    }
}
