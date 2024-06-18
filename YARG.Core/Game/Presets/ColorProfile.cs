using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace YARG.Core.Game
{
    public struct YARGColor
    {
        public static readonly YARGColor White = new()
        {
            R = 255,
            G = 255,
            B = 255,
            A = 255,
        };

        public static YARGColor FromArgb(int color)
        {
            return new YARGColor()
            {
                A = (byte)(color & 0xFF),
                R = (byte)((color >> 8) & 0xFF),
                G = (byte)((color >> 16) & 0xFF),
                B = (byte)((color >> 24) & 0xFF)
            };
        }

        public static YARGColor FromArgb(int a, int r, int g, int b)
        {
            return new YARGColor()
            {
                 R = (byte)r,
                 G = (byte)g,
                 B = (byte)b,
                 A = (byte)a
            };
        }

        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public readonly int ToArgb()
        {
            return A | (R << 8) | (G << 16) | (B << 24);
        }
    }

    public partial struct ColorProfile
    {
        public const int COLOR_PROFILE_VERSION = 1;

        /// <summary>
        /// Interface that has methods that allows for generic fret color retrieval.
        /// Not all instruments have frets, so it's an interface.
        /// </summary>
        public interface IFretColorProvider
        {
            #region Default Colors

            public static readonly YARGColor DefaultPurple = YARGColor.FromArgb(0xFF, 0xC8, 0x00, 0xFF); // #C800FF
            public static readonly YARGColor DefaultGreen = YARGColor.FromArgb(0xFF, 0x79, 0xD3, 0x04); // #79D304
            public static readonly YARGColor DefaultRed = YARGColor.FromArgb(0xFF, 0xFF, 0x1D, 0x23); // #FF1D23
            public static readonly YARGColor DefaultYellow = YARGColor.FromArgb(0xFF, 0xFF, 0xE9, 0x00); // #FFE900
            public static readonly YARGColor DefaultBlue = YARGColor.FromArgb(0xFF, 0x00, 0xBF, 0xFF); // #00BFFF
            public static readonly YARGColor DefaultOrange = YARGColor.FromArgb(0xFF, 0xFF, 0x84, 0x00); // #FF8400

            public static readonly YARGColor DefaultStarpower = YARGColor.White; // #FFFFFF

            #endregion

            #region Circular Colors

            public static readonly YARGColor CircularPurple = YARGColor.FromArgb(0xFF, 0xBE, 0x0F, 0xFF); // #BE0FFF
            public static readonly YARGColor CircularGreen = YARGColor.FromArgb(0xFF, 0x00, 0xC9, 0x0E); // #00C90E
            public static readonly YARGColor CircularRed = YARGColor.FromArgb(0xFF, 0xC3, 0x00, 0x00); // #C30000
            public static readonly YARGColor CircularYellow = YARGColor.FromArgb(0xFF, 0xF5, 0xD0, 0x00); // #F5D000
            public static readonly YARGColor CircularBlue = YARGColor.FromArgb(0xFF, 0x00, 0x5C, 0xF5); // #005CF5
            public static readonly YARGColor CircularOrange = YARGColor.FromArgb(0xFF, 0xFF, 0x84, 0x00); // #FF8400

            public static readonly YARGColor CircularStarpower = YARGColor.FromArgb(0xFF, 0x13, 0xD9, 0xEA); // #13D9EA

            #endregion

            #region April Fools Colors

            public static readonly YARGColor AprilFoolsGreen = YARGColor.FromArgb(0xFF, 0x24, 0xB9, 0x00); // #24B900
            public static readonly YARGColor AprilFoolsRed = YARGColor.FromArgb(0xFF, 0xD1, 0x13, 0x00); // #D11300
            public static readonly YARGColor AprilFoolsYellow = YARGColor.FromArgb(0xFF, 0xD1, 0xA7, 0x00); // #D1A700
            public static readonly YARGColor AprilFoolsBlue = YARGColor.FromArgb(0xFF, 0x00, 0x1A, 0xDC); // #001ADC
            public static readonly YARGColor AprilFoolsPurple = YARGColor.FromArgb(0xFF, 0xEB, 0x00, 0xD1); // #EB00D1

            #endregion

            public YARGColor GetFretColor(int index);
            public YARGColor GetFretInnerColor(int index);
            public YARGColor GetParticleColor(int index);
        }

        public FiveFretGuitarColors FiveFretGuitar;
        public FourLaneDrumsColors FourLaneDrums;
        public FiveLaneDrumsColors FiveLaneDrums;
    }
}