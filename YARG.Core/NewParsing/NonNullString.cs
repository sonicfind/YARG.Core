using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// A wrapper around a string that guarantees at minimum a empty string on conversion.
    /// </summary>
    /// <remarks>
    /// This is necessary for use with generic containers that require the type to come with a default constructor
    /// </remarks>
    public struct NonNullString
    {
        private string _value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() { return _value ?? string.Empty; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(in NonNullString str) => str._value ?? string.Empty;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NonNullString(string str) => new() { _value = str };
    }
}
