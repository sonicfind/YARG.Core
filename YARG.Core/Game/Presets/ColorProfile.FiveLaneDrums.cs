using System;
using System.Drawing;
using System.IO;
using YARG.Core.Extensions;
using YARG.Core.Utility;

namespace YARG.Core.Game
{
    public partial struct ColorProfile
    {
        public struct FiveLaneDrumsColors : IFretColorProvider
        {
            public static readonly FiveLaneDrumsColors Default = new()
            {
                KickFret = YARGColor.FromArgb(0xFF, 0xE6, 0x3F, 0xFF),
                RedFret = IFretColorProvider.DefaultRed,
                YellowFret = IFretColorProvider.DefaultYellow,
                BlueFret = IFretColorProvider.DefaultBlue,
                OrangeFret = IFretColorProvider.DefaultOrange,
                GreenFret = IFretColorProvider.DefaultGreen,

                KickFretInner = IFretColorProvider.DefaultOrange,
                RedFretInner = IFretColorProvider.DefaultRed,
                YellowFretInner = IFretColorProvider.DefaultYellow,
                BlueFretInner = IFretColorProvider.DefaultBlue,
                OrangeFretInner = IFretColorProvider.DefaultOrange,
                GreenFretInner = IFretColorProvider.DefaultGreen,

                KickParticles = YARGColor.FromArgb(0xFF, 0xD5, 0x00, 0xFF),
                RedParticles = IFretColorProvider.DefaultRed,
                YellowParticles = IFretColorProvider.DefaultYellow,
                BlueParticles = IFretColorProvider.DefaultBlue,
                OrangeParticles = IFretColorProvider.DefaultOrange,
                GreenParticles = IFretColorProvider.DefaultGreen,

                KickNote = IFretColorProvider.DefaultOrange,
                RedNote = IFretColorProvider.DefaultRed,
                YellowNote = IFretColorProvider.DefaultYellow,
                BlueNote = IFretColorProvider.DefaultBlue,
                OrangeNote = IFretColorProvider.DefaultOrange,
                GreenNote = IFretColorProvider.DefaultGreen,

                KickStarpower = IFretColorProvider.DefaultStarpower,
                RedStarpower = IFretColorProvider.DefaultStarpower,
                YellowStarpower = IFretColorProvider.DefaultStarpower,
                BlueStarpower = IFretColorProvider.DefaultStarpower,
                OrangeStarpower = IFretColorProvider.DefaultStarpower,
                GreenStarpower = IFretColorProvider.DefaultStarpower,

                ActivationNote = IFretColorProvider.DefaultPurple,
            };


            #region Frets

            public YARGColor KickFret; // #E63FFF;
            public YARGColor RedFret;
            public YARGColor YellowFret;
            public YARGColor BlueFret;
            public YARGColor OrangeFret;
            public YARGColor GreenFret;

            /// <summary>
            /// Gets the fret color for a specific note index.
            /// 0 = kick note, 1 = red, 5 = green.
            /// </summary>
            public readonly YARGColor GetFretColor(int index)
            {
                return index switch
                {
                    0 => KickFret,
                    1 => RedFret,
                    2 => YellowFret,
                    3 => BlueFret,
                    4 => OrangeFret,
                    5 => GreenFret,
                    _ => default
                };
            }

            public YARGColor KickFretInner;
            public YARGColor RedFretInner;
            public YARGColor YellowFretInner;
            public YARGColor BlueFretInner;
            public YARGColor OrangeFretInner;
            public YARGColor GreenFretInner;

            /// <summary>
            /// Gets the inner fret color for a specific note index.
            /// 0 = kick note, 1 = red, 5 = green.
            /// </summary>
            public readonly YARGColor GetFretInnerColor(int index)
            {
                return index switch
                {
                    0 => KickFretInner,
                    1 => RedFretInner,
                    2 => YellowFretInner,
                    3 => BlueFretInner,
                    4 => OrangeFretInner,
                    5 => GreenFretInner,
                    _ => default
                };
            }

            public YARGColor KickParticles; // #D500FF
            public YARGColor RedParticles;
            public YARGColor YellowParticles;
            public YARGColor BlueParticles;
            public YARGColor OrangeParticles;
            public YARGColor GreenParticles;

            /// <summary>
            /// Gets the particle color for a specific note index.
            /// 0 = kick note, 1 = red, 5 = green.
            /// </summary>
            public readonly YARGColor GetParticleColor(int index)
            {
                return index switch
                {
                    0 => KickParticles,
                    1 => RedParticles,
                    2 => YellowParticles,
                    3 => BlueParticles,
                    4 => OrangeParticles,
                    5 => GreenParticles,
                    _ => default
                };
            }

            #endregion

            #region Notes

            public YARGColor KickNote;

            public YARGColor RedNote;
            public YARGColor YellowNote;
            public YARGColor BlueNote;
            public YARGColor OrangeNote;
            public YARGColor GreenNote;

            /// <summary>
            /// Gets the note color for a specific note index.
            /// 0 = kick note, 1 = red drum, 5 = green drum.
            /// </summary>
            public readonly YARGColor GetNoteColor(int index)
            {
                return index switch
                {
                    0 => KickNote,

                    1 => RedNote,
                    2 => YellowNote,
                    3 => BlueNote,
                    4 => OrangeNote,
                    5 => GreenNote,

                    _ => default
                };
            }

            public YARGColor KickStarpower;

            public YARGColor RedStarpower;
            public YARGColor YellowStarpower;
            public YARGColor BlueStarpower;
            public YARGColor OrangeStarpower;
            public YARGColor GreenStarpower;

            /// <summary>
            /// Gets the Star Power note color for a specific note index.
            /// 0 = kick note, 1 = red drum, 5 = green drum.
            /// </summary>
            public readonly YARGColor GetNoteStarPowerColor(int index)
            {
                return index switch
                {
                    0 => KickStarpower,

                    1 => RedStarpower,
                    2 => YellowStarpower,
                    3 => BlueStarpower,
                    4 => OrangeStarpower,
                    5 => GreenStarpower,

                    _ => default
                };
            }

            public YARGColor ActivationNote;

            #endregion

            #region Serialization

            public FiveLaneDrumsColors(BinaryReader reader)
            {
                KickFret = reader.ReadColor();
                RedFret = reader.ReadColor();
                YellowFret = reader.ReadColor();
                BlueFret = reader.ReadColor();
                OrangeFret = reader.ReadColor();
                GreenFret = reader.ReadColor();

                KickFretInner = reader.ReadColor();
                RedFretInner = reader.ReadColor();
                YellowFretInner = reader.ReadColor();
                BlueFretInner = reader.ReadColor();
                OrangeFretInner = reader.ReadColor();
                GreenFretInner = reader.ReadColor();

                KickParticles = reader.ReadColor();
                RedParticles = reader.ReadColor();
                YellowParticles = reader.ReadColor();
                BlueParticles = reader.ReadColor();
                OrangeParticles = reader.ReadColor();
                GreenParticles = reader.ReadColor();

                KickNote = reader.ReadColor();
                RedNote = reader.ReadColor();
                YellowNote = reader.ReadColor();
                BlueNote = reader.ReadColor();
                OrangeNote = reader.ReadColor();
                GreenNote = reader.ReadColor();

                KickStarpower = reader.ReadColor();
                RedStarpower = reader.ReadColor();
                YellowStarpower = reader.ReadColor();
                BlueStarpower = reader.ReadColor();
                OrangeStarpower = reader.ReadColor();
                GreenStarpower = reader.ReadColor();

                ActivationNote = reader.ReadColor();
            }

            public readonly void Serialize(BinaryWriter writer)
            {
                writer.Write(KickFret);
                writer.Write(RedFret);
                writer.Write(YellowFret);
                writer.Write(BlueFret);
                writer.Write(OrangeFret);
                writer.Write(GreenFret);

                writer.Write(KickFretInner);
                writer.Write(RedFretInner);
                writer.Write(YellowFretInner);
                writer.Write(BlueFretInner);
                writer.Write(OrangeFretInner);
                writer.Write(GreenFretInner);

                writer.Write(KickParticles);
                writer.Write(RedParticles);
                writer.Write(YellowParticles);
                writer.Write(BlueParticles);
                writer.Write(OrangeParticles);
                writer.Write(GreenParticles);

                writer.Write(KickNote);
                writer.Write(RedNote);
                writer.Write(YellowNote);
                writer.Write(BlueNote);
                writer.Write(OrangeNote);
                writer.Write(GreenNote);

                writer.Write(KickStarpower);
                writer.Write(RedStarpower);
                writer.Write(YellowStarpower);
                writer.Write(BlueStarpower);
                writer.Write(OrangeStarpower);
                writer.Write(GreenStarpower);

                writer.Write(ActivationNote);
            }
            #endregion
        }
    }
}