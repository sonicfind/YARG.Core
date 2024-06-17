using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace YARG.Core.Game
{
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

            public static readonly Color DefaultPurple = Color.FromArgb(0xFF, 0xC8, 0x00, 0xFF); // #C800FF
            public static readonly Color DefaultGreen = Color.FromArgb(0xFF, 0x79, 0xD3, 0x04); // #79D304
            public static readonly Color DefaultRed = Color.FromArgb(0xFF, 0xFF, 0x1D, 0x23); // #FF1D23
            public static readonly Color DefaultYellow = Color.FromArgb(0xFF, 0xFF, 0xE9, 0x00); // #FFE900
            public static readonly Color DefaultBlue = Color.FromArgb(0xFF, 0x00, 0xBF, 0xFF); // #00BFFF
            public static readonly Color DefaultOrange = Color.FromArgb(0xFF, 0xFF, 0x84, 0x00); // #FF8400

            public static readonly Color DefaultStarpower = Color.White; // #FFFFFF

            #endregion

            #region Circular Colors

            public static readonly Color CircularPurple = Color.FromArgb(0xFF, 0xBE, 0x0F, 0xFF); // #BE0FFF
            public static readonly Color CircularGreen = Color.FromArgb(0xFF, 0x00, 0xC9, 0x0E); // #00C90E
            public static readonly Color CircularRed = Color.FromArgb(0xFF, 0xC3, 0x00, 0x00); // #C30000
            public static readonly Color CircularYellow = Color.FromArgb(0xFF, 0xF5, 0xD0, 0x00); // #F5D000
            public static readonly Color CircularBlue = Color.FromArgb(0xFF, 0x00, 0x5C, 0xF5); // #005CF5
            public static readonly Color CircularOrange = Color.FromArgb(0xFF, 0xFF, 0x84, 0x00); // #FF8400

            public static readonly Color CircularStarpower = Color.FromArgb(0xFF, 0x13, 0xD9, 0xEA); // #13D9EA

            #endregion

            #region April Fools Colors

            public static readonly Color AprilFoolsGreen = Color.FromArgb(0xFF, 0x24, 0xB9, 0x00); // #24B900
            public static readonly Color AprilFoolsRed = Color.FromArgb(0xFF, 0xD1, 0x13, 0x00); // #D11300
            public static readonly Color AprilFoolsYellow = Color.FromArgb(0xFF, 0xD1, 0xA7, 0x00); // #D1A700
            public static readonly Color AprilFoolsBlue = Color.FromArgb(0xFF, 0x00, 0x1A, 0xDC); // #001ADC
            public static readonly Color AprilFoolsPurple = Color.FromArgb(0xFF, 0xEB, 0x00, 0xD1); // #EB00D1

            #endregion

            public Color GetFretColor(int index);
            public Color GetFretInnerColor(int index);
            public Color GetParticleColor(int index);
        }

        public FiveFretGuitarColors FiveFretGuitar;
        public FourLaneDrumsColors FourLaneDrums;
        public FiveLaneDrumsColors FiveLaneDrums;
    }
}