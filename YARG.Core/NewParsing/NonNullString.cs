namespace YARG.Core.NewParsing
{
    public class NonNullString
    {
        private string _str;

        public string Str
        {
            get => _str;
            set => _str = value ?? string.Empty;
        }

        public NonNullString()
        {
            _str = string.Empty;
        }

        public NonNullString(string str)
        {
            _str = str ?? string.Empty;
        }

        public static implicit operator string(NonNullString section) => section._str;
        public static implicit operator NonNullString(string str) => new(str);
    }
}
