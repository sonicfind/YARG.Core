using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Utility;

namespace YARG.Core.IO.Ini
{
    public enum ModifierType
    {
        None,
        String,
        UInt64,
        Int64,
        UInt32,
        Int32,
        UInt16,
        Int16,
        Bool,
        Float,
        Double,
        Int64Array,
    };

    public readonly struct IniModifierOutline
    {
        public readonly string Output;
        public readonly ModifierType Type;

        public IniModifierOutline(string output, ModifierType type)
        {
            Output = output;
            Type = type;
        }
    };

    public class IniModifierCollection
    {
        private Dictionary<string, string> _strings = new(); 
        private Dictionary<string, ulong> _uint64s = new(); 
        private Dictionary<string, long> _int64s = new(); 
        private Dictionary<string, uint> _uint32s = new(); 
        private Dictionary<string, int> _int32s = new(); 
        private Dictionary<string, ushort> _uint16s = new(); 
        private Dictionary<string, short> _int16s = new(); 
        private Dictionary<string, bool> _booleans = new(); 
        private Dictionary<string, float> _floats = new(); 
        private Dictionary<string, double> _doubles = new(); 
        private Dictionary<string, (long, long)> _int64Arrays = new(); 

        public bool Contains(string key)
        {
            return _strings.ContainsKey(key)
                || _uint64s.ContainsKey(key)
                || _int64s.ContainsKey(key)
                || _uint32s.ContainsKey(key)
                || _int32s.ContainsKey(key)
                || _uint16s.ContainsKey(key)
                || _int16s.ContainsKey(key)
                || _booleans.ContainsKey(key)
                || _floats.ContainsKey(key)
                || _doubles.ContainsKey(key)
                || _int64Arrays.ContainsKey(key);
        }

        public void Add<TChar>(ref YARGTextContainer<TChar> container, in IniModifierOutline outline, bool isChartFile)
            where TChar : unmanaged, IConvertible
        {
            switch (outline.Type)
            {
                case ModifierType.String:
                    _strings[outline.Output] = RichTextUtils.ReplaceColorNames(YARGTextReader.ExtractText(ref container, isChartFile));
                    break;
                case ModifierType.UInt64:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out ulong value))
                        {
                            value = 0;
                        }
                        _uint64s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int64:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out long value))
                        {
                            value = 0;
                        }
                        _int64s[outline.Output] = value;
                        break;
                    }
                case ModifierType.UInt32:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out uint value))
                        {
                            value = 0;
                        }
                        _uint32s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int32:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out int value))
                        {
                            value = 0;
                        }
                        _int32s[outline.Output] = value;
                        break;
                    }
                case ModifierType.UInt16:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out ushort value))
                        {
                            value = 0;
                        }
                        _uint16s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int16:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out short value))
                        {
                            value = 0;
                        }
                        _int16s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Bool:
                    _booleans[outline.Output] = YARGTextReader.ExtractBoolean(in container);
                    break;
                case ModifierType.Float:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out float value))
                        {
                            value = 0;
                        }
                        _floats[outline.Output] = value;
                        break;
                    }
                case ModifierType.Double:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out double value))
                        {
                            value = 0;
                        }
                        _doubles[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int64Array:
                    long i641, i642;
                    if (YARGTextReader.TryExtract(ref container, out i641))
                    {
                        YARGTextReader.SkipWhitespaceAndEquals(ref container);
                        if (!YARGTextReader.TryExtract(ref container, out i642))
                        {
                            i642 = -1;
                        }
                    }
                    else
                    {
                        i641 = -1;
                        i642 = -1;
                    }
                    _int64Arrays[outline.Output] = (i641, i642);
                    break;
            }
        }

        public void AddSng(ref YARGTextContainer<byte> container, int length, in IniModifierOutline outline)
        {
            switch (outline.Type)
            {
                case ModifierType.String:
                    unsafe
                    {
                        _strings[outline.Output] = RichTextUtils.ReplaceColorNames(Encoding.UTF8.GetString(container.PositionPointer, length));
                    }
                    break;
                case ModifierType.UInt64:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out ulong value))
                        {
                            value = 0;
                        }
                        _uint64s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int64:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out long value))
                        {
                            value = 0;
                        }
                        _int64s[outline.Output] = value;
                        break;
                    }
                case ModifierType.UInt32:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out uint value))
                        {
                            value = 0;
                        }
                        _uint32s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int32:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out int value))
                        {
                            value = 0;
                        }
                        _int32s[outline.Output] = value;
                        break;
                    }
                case ModifierType.UInt16:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out ushort value))
                        {
                            value = 0;
                        }
                        _uint16s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int16:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out short value))
                        {
                            value = 0;
                        }
                        _int16s[outline.Output] = value;
                        break;
                    }
                case ModifierType.Bool:
                    _booleans[outline.Output] = YARGTextReader.ExtractBoolean(in container);
                    break;
                case ModifierType.Float:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out float value))
                        {
                            value = 0;
                        }
                        _floats[outline.Output] = value;
                        break;
                    }
                case ModifierType.Double:
                    {
                        if (!YARGTextReader.TryExtract(ref container, out double value))
                        {
                            value = 0;
                        }
                        _doubles[outline.Output] = value;
                        break;
                    }
                case ModifierType.Int64Array:
                    long i641, i642;
                    if (!YARGTextReader.TryExtractWithWhitespace(ref container, out i641))
                    {
                        i641 = -1;
                        i642 = -1;
                    }
                    else if (!YARGTextReader.TryExtract(ref container, out i642))
                    {
                        i642 = -1;
                    }
                    _int64Arrays[outline.Output] = (i641, i642);
                    break;
            }
        }

        public void Union(IniModifierCollection source)
        {
            Union(_strings, source._strings);
            Union(_uint64s, source._uint64s);
            Union(_int64s, source._int64s);
            Union(_uint32s, source._uint32s);
            Union(_int32s, source._int32s);
            Union(_uint16s, source._uint16s);
            Union(_int16s, source._int16s);
            Union(_booleans, source._booleans);
            Union(_floats, source._floats);
            Union(_doubles, source._doubles);
            Union(_int64Arrays, source._int64Arrays);
        }

        public bool IsEmpty()
        {
            return _strings.Count == 0
                && _uint64s.Count == 0
                && _int64s.Count == 0
                && _uint32s.Count == 0
                && _int32s.Count == 0
                && _uint16s.Count == 0
                && _int16s.Count == 0
                && _booleans.Count == 0
                && _floats.Count == 0
                && _doubles.Count == 0
                && _int64Arrays.Count == 0;
        }

        public bool Extract(string key, out string value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.String, "Mismatched modifier types - String requested");
#endif
            return _strings.Remove(key, out value);
        }

        public bool Extract(string key, out ulong value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.UInt64, "Mismatched modifier types - UInt64 requested");
#endif
            return _uint64s.Remove(key, out value);
        }

        public bool Extract(string key, out long value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Int64, "Mismatched modifier types - Int64 requested");
#endif
            return _int64s.Remove(key, out value);
        }

        public bool Extract(string key, out uint value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.UInt32, "Mismatched modifier types - UInt32 requested");
#endif
            return _uint32s.Remove(key, out value);
        }

        public bool Extract(string key, out int value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Int32, "Mismatched modifier types - Int32 requested");
#endif
            return _int32s.Remove(key, out value);
        }

        public bool Extract(string key, out ushort value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.UInt16, "Mismatched modifier types - UInt16 requested");
#endif
            return _uint16s.Remove(key, out value);
        }

        public bool Extract(string key, out short value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Int16, "Mismatched modifier types - Int16 requested");
#endif
            return _int16s.Remove(key, out value);
        }

        public bool Extract(string key, out bool value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Bool, "Mismatched modifier types - Boolean requested");
#endif
            return _booleans.Remove(key, out value);
        }

        public bool Extract(string key, out float value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Float, "Mismatched modifier types - Float requested");
#endif
            return _floats.Remove(key, out value);
        }

        public bool Extract(string key, out double value)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Double, "Mismatched modifier types - Double requested");
#endif
            return _doubles.Remove(key, out value);
        }

        public bool Extract(string key, out (long, long) values)
        {
#if DEBUG
            ThrowIfMismatch(key, ModifierType.Int64Array, "Mismatched modifier types - Int64Array requested");
#endif
            return _int64Arrays.Remove(key, out values);
        }

        private static void Union<TValue>(Dictionary<string, TValue> dest, Dictionary<string, TValue> source)
        {
            foreach (var node in source)
            {
                dest[node.Key] = node.Value;
            }
        }

#if DEBUG
        private static Dictionary<string, ModifierType> debug_validation = new();
        private static void ThrowIfMismatch(string key, ModifierType type, string error)
        {
            lock (debug_validation)
            {
                if (debug_validation.TryGetValue(key, out var value) && value != type)
                {
                    throw new InvalidOperationException(error);
                }
                debug_validation[key] = type;
            }
        }
#endif
    }
}
