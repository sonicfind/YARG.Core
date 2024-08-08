using System.Diagnostics;

namespace YARG.Core.NewParsing
{
    [DebuggerDisplay("{_str}")]
    /// <remarks>
    /// If given the chance, using newer C# standards, I would like to change this to a struct.
    /// C#10 supports paramaterless constructors for struct types, which would be of great use for this type.
    /// </remarks>
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
