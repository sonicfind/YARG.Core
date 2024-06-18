using System.Drawing;
using System.IO;
using YARG.Core.Extensions;
using YARG.Core.Utility;

namespace YARG.Core.Game
{
    public partial struct ColorProfile
    {
        public struct FiveFretGuitarColors : IFretColorProvider
        {
            public static readonly FiveFretGuitarColors Default = new()
            {
                OpenFret = IFretColorProvider.DefaultPurple,
                GreenFret = IFretColorProvider.DefaultGreen,
                RedFret = IFretColorProvider.DefaultRed,
                YellowFret = IFretColorProvider.DefaultYellow,
                BlueFret = IFretColorProvider.DefaultBlue,
                OrangeFret = IFretColorProvider.DefaultOrange,

                OpenFretInner = IFretColorProvider.DefaultPurple,
                GreenFretInner = IFretColorProvider.DefaultGreen,
                RedFretInner = IFretColorProvider.DefaultRed,
                YellowFretInner = IFretColorProvider.DefaultYellow,
                BlueFretInner = IFretColorProvider.DefaultBlue,
                OrangeFretInner = IFretColorProvider.DefaultOrange,

                OpenParticles = IFretColorProvider.DefaultPurple,
                GreenParticles = IFretColorProvider.DefaultGreen,
                RedParticles = IFretColorProvider.DefaultRed,
                YellowParticles = IFretColorProvider.DefaultYellow,
                BlueParticles = IFretColorProvider.DefaultBlue,
                OrangeParticles = IFretColorProvider.DefaultOrange,

                OpenNote = IFretColorProvider.DefaultPurple,
                GreenNote = IFretColorProvider.DefaultGreen,
                RedNote = IFretColorProvider.DefaultRed,
                YellowNote = IFretColorProvider.DefaultYellow,
                BlueNote = IFretColorProvider.DefaultBlue,
                OrangeNote = IFretColorProvider.DefaultOrange,

                OpenNoteStarPower = IFretColorProvider.DefaultStarpower,
                GreenNoteStarPower = IFretColorProvider.DefaultStarpower,
                RedNoteStarPower = IFretColorProvider.DefaultStarpower,
                YellowNoteStarPower = IFretColorProvider.DefaultStarpower,
                BlueNoteStarPower = IFretColorProvider.DefaultStarpower,
                OrangeNoteStarPower = IFretColorProvider.DefaultStarpower,
            };

            #region Frets

            public YARGColor OpenFret;
            public YARGColor GreenFret;
            public YARGColor RedFret;
            public YARGColor YellowFret;
            public YARGColor BlueFret;
            public YARGColor OrangeFret;

            /// <summary>
            /// Gets the fret color for a specific note index.
            /// 0 = open note, 1 = green, 5 = orange.
            /// </summary>
            public readonly YARGColor GetFretColor(int index)
            {
                return index switch
                {
                    0 => OpenFret,
                    1 => GreenFret,
                    2 => RedFret,
                    3 => YellowFret,
                    4 => BlueFret,
                    5 => OrangeFret,
                    _ => default
                };
            }

            public YARGColor OpenFretInner;
            public YARGColor GreenFretInner;
            public YARGColor RedFretInner;
            public YARGColor YellowFretInner;
            public YARGColor BlueFretInner;
            public YARGColor OrangeFretInner;

            /// <summary>
            /// Gets the inner fret color for a specific note index.
            /// 0 = open note, 1 = green, 5 = orange.
            /// </summary>
            public readonly YARGColor GetFretInnerColor(int index)
            {
                return index switch
                {
                    0 => OpenFretInner,
                    1 => GreenFretInner,
                    2 => RedFretInner,
                    3 => YellowFretInner,
                    4 => BlueFretInner,
                    5 => OrangeFretInner,
                    _ => default
                };
            }

            public YARGColor OpenParticles;
            public YARGColor GreenParticles;
            public YARGColor RedParticles;
            public YARGColor YellowParticles;
            public YARGColor BlueParticles;
            public YARGColor OrangeParticles;

            /// <summary>
            /// Gets the particle color for a specific note index.
            /// 0 = open note, 1 = green, 5 = orange.
            /// </summary>
            public readonly YARGColor GetParticleColor(int index)
            {
                return index switch
                {
                    0 => OpenParticles,
                    1 => GreenParticles,
                    2 => RedParticles,
                    3 => YellowParticles,
                    4 => BlueParticles,
                    5 => OrangeParticles,
                    _ => default
                };
            }

            #endregion

            #region Notes

            public YARGColor OpenNote;
            public YARGColor GreenNote;
            public YARGColor RedNote;
            public YARGColor YellowNote;
            public YARGColor BlueNote;
            public YARGColor OrangeNote;

            /// <summary>
            /// Gets the note color for a specific note index.
            /// 0 = open note, 1 = green, 5 = orange.
            /// </summary>
            public readonly YARGColor GetNoteColor(int index)
            {
                return index switch
                {
                    0 => OpenNote,
                    1 => GreenNote,
                    2 => RedNote,
                    3 => YellowNote,
                    4 => BlueNote,
                    5 => OrangeNote,
                    _ => default
                };
            }

            public YARGColor OpenNoteStarPower;
            public YARGColor GreenNoteStarPower;
            public YARGColor RedNoteStarPower;
            public YARGColor YellowNoteStarPower;
            public YARGColor BlueNoteStarPower;
            public YARGColor OrangeNoteStarPower;

            /// <summary>
            /// Gets the Star Power note color for a specific note index.
            /// 0 = open note, 1 = green, 5 = orange.
            /// </summary>
            public readonly YARGColor GetNoteStarPowerColor(int index)
            {
                return index switch
                {
                    0 => OpenNoteStarPower,
                    1 => GreenNoteStarPower,
                    2 => RedNoteStarPower,
                    3 => YellowNoteStarPower,
                    4 => BlueNoteStarPower,
                    5 => OrangeNoteStarPower,
                    _ => default
                };
            }

            #endregion

            #region Serialization

            public FiveFretGuitarColors(BinaryReader reader)
            {
                OpenFret = reader.ReadColor();
                GreenFret = reader.ReadColor();
                RedFret = reader.ReadColor();
                YellowFret = reader.ReadColor();
                BlueFret = reader.ReadColor();
                OrangeFret = reader.ReadColor();

                OpenFretInner = reader.ReadColor();
                GreenFretInner = reader.ReadColor();
                RedFretInner = reader.ReadColor();
                YellowFretInner = reader.ReadColor();
                BlueFretInner = reader.ReadColor();
                OrangeFretInner = reader.ReadColor();

                OpenParticles = reader.ReadColor();
                GreenParticles = reader.ReadColor();
                RedParticles = reader.ReadColor();
                YellowParticles = reader.ReadColor();
                BlueParticles = reader.ReadColor();
                OrangeParticles = reader.ReadColor();

                OpenNote = reader.ReadColor();
                GreenNote = reader.ReadColor();
                RedNote = reader.ReadColor();
                YellowNote = reader.ReadColor();
                BlueNote = reader.ReadColor();
                OrangeNote = reader.ReadColor();

                OpenNoteStarPower = reader.ReadColor();
                GreenNoteStarPower = reader.ReadColor();
                RedNoteStarPower = reader.ReadColor();
                YellowNoteStarPower = reader.ReadColor();
                BlueNoteStarPower = reader.ReadColor();
                OrangeNoteStarPower = reader.ReadColor();
            }

            public readonly void Serialize(BinaryWriter writer)
            {
                writer.Write(OpenFret);
                writer.Write(GreenFret);
                writer.Write(RedFret);
                writer.Write(YellowFret);
                writer.Write(BlueFret);
                writer.Write(OrangeFret);

                writer.Write(OpenFretInner);
                writer.Write(GreenFretInner);
                writer.Write(RedFretInner);
                writer.Write(YellowFretInner);
                writer.Write(BlueFretInner);
                writer.Write(OrangeFretInner);

                writer.Write(OpenParticles);
                writer.Write(GreenParticles);
                writer.Write(RedParticles);
                writer.Write(YellowParticles);
                writer.Write(BlueParticles);
                writer.Write(OrangeParticles);

                writer.Write(OpenNote);
                writer.Write(GreenNote);
                writer.Write(RedNote);
                writer.Write(YellowNote);
                writer.Write(BlueNote);
                writer.Write(OrangeNote);

                writer.Write(OpenNoteStarPower);
                writer.Write(GreenNoteStarPower);
                writer.Write(RedNoteStarPower);
                writer.Write(YellowNoteStarPower);
                writer.Write(BlueNoteStarPower);
                writer.Write(OrangeNoteStarPower);
            }

            #endregion
        }
    }
}