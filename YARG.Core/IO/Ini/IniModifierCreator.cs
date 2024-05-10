﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YARG.Core.Song;

namespace YARG.Core.IO.Ini
{
    public enum ModifierCreatorType
    {
        None,
        SortString,
        String,
        SortString_Chart,
        String_Chart,
        UInt64,
        Int64,
        UInt32,
        Int32,
        UInt16,
        Int16,
        Bool,
        Float,
        Double,
        UInt64Array,
    }

    public sealed class IniModifierCreator
    {
        public readonly string outputName;
        public readonly ModifierCreatorType type;

        public IniModifierCreator(string outputName, ModifierCreatorType type)
        {
            this.outputName = outputName;
            this.type = type;
        }

        public IniModifier CreateModifier<TChar, TDecoder>(YARGTextContainer<TChar> container, TDecoder decoder)
            where TChar : unmanaged, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
        {
            return type switch
            {
                ModifierCreatorType.SortString       => new IniModifier(new SortString(YARGTextReader.ExtractText(container, decoder, false))),
                ModifierCreatorType.SortString_Chart => new IniModifier(new SortString(YARGTextReader.ExtractText(container, decoder, true))),
                ModifierCreatorType.String           => new IniModifier(YARGTextReader.ExtractText(container, decoder, false)),
                ModifierCreatorType.String_Chart     => new IniModifier(YARGTextReader.ExtractText(container, decoder, true)),
                _ => CreateNumberModifier(container),
            };
        }

        public IniModifier CreateSngModifier(YARGTextContainer<byte> sngContainer, int length)
        {
            return type switch
            {
                ModifierCreatorType.SortString => new IniModifier(new SortString(Encoding.UTF8.GetString(sngContainer.Data, sngContainer.Position, length))),
                ModifierCreatorType.String => new IniModifier(Encoding.UTF8.GetString(sngContainer.Data, sngContainer.Position, length)),
                _ => CreateNumberModifier(sngContainer),
            };
        }

        private IniModifier CreateNumberModifier<TChar>(YARGTextContainer<TChar> container)
            where TChar : unmanaged, IConvertible
        {
            switch (type)
            {
                case ModifierCreatorType.UInt64:
                    {
                        container.TryExtractUInt64(out ulong value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.Int64:
                    {
                        container.TryExtractInt64(out long value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.UInt32:
                    {
                        container.TryExtractUInt32(out uint value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.Int32:
                    {
                        container.TryExtractInt32(out int value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.UInt16:
                    {
                        container.TryExtractUInt16(out ushort value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.Int16:
                    {
                        container.TryExtractInt16(out short value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.Bool:
                    {
                        return new IniModifier(container.ExtractBoolean());
                    }
                case ModifierCreatorType.Float:
                    {
                        container.TryExtractFloat(out float value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.Double:
                    {
                        container.TryExtractDouble(out double value);
                        return new IniModifier(value);
                    }
                case ModifierCreatorType.UInt64Array:
                    {
                        long l2 = -1;
                        if (container.TryExtractInt64(out long l1))
                        {
                            YARGTextReader.SkipWhitespace(container);
                            if (!container.TryExtractInt64(out l2))
                            {
                                l2 = -1;
                            }
                        }
                        else
                        {
                            l1 = -1;
                        }
                        return new IniModifier(l1, l2);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
