using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;

namespace YARG.Core.Game
{
    public partial struct ColorProfile
    {
        public static readonly PresetContainer<ColorProfile> Default = new("Default", new ColorProfile()
        {
            FiveFretGuitar = FiveFretGuitarColors.Default,
            FourLaneDrums = FourLaneDrumsColors.Default,
            FiveLaneDrums = FiveLaneDrumsColors.Default,
        });

        public static readonly PresetContainer<ColorProfile> CircularDefault = new("Circular", new ColorProfile()
        {
            FiveFretGuitar = new FiveFretGuitarColors()
            {
                OpenFret   = IFretColorProvider.CircularPurple,
                GreenFret  = IFretColorProvider.CircularGreen,
                RedFret    = IFretColorProvider.CircularRed,
                YellowFret = IFretColorProvider.CircularYellow,
                BlueFret   = IFretColorProvider.CircularBlue,
                OrangeFret = IFretColorProvider.CircularOrange,

                OpenFretInner   = IFretColorProvider.CircularPurple,
                GreenFretInner  = IFretColorProvider.CircularGreen,
                RedFretInner    = IFretColorProvider.CircularRed,
                YellowFretInner = IFretColorProvider.CircularYellow,
                BlueFretInner   = IFretColorProvider.CircularBlue,
                OrangeFretInner = IFretColorProvider.CircularOrange,

                OpenNote   = IFretColorProvider.CircularPurple,
                GreenNote  = IFretColorProvider.CircularGreen,
                RedNote    = IFretColorProvider.CircularRed,
                YellowNote = IFretColorProvider.CircularYellow,
                BlueNote   = IFretColorProvider.CircularBlue,
                OrangeNote = IFretColorProvider.CircularOrange,

                OpenNoteStarPower   = IFretColorProvider.CircularStarpower,
                GreenNoteStarPower  = IFretColorProvider.CircularStarpower,
                RedNoteStarPower    = IFretColorProvider.CircularStarpower,
                YellowNoteStarPower = IFretColorProvider.CircularStarpower,
                BlueNoteStarPower   = IFretColorProvider.CircularStarpower,
                OrangeNoteStarPower = IFretColorProvider.CircularStarpower,
            },
            FourLaneDrums = FourLaneDrumsColors.Default,
            FiveLaneDrums = FiveLaneDrumsColors.Default,
        });

        public static readonly PresetContainer<ColorProfile> AprilFoolsDefault = new("YARG on Fire", new ColorProfile()
        {
            FiveFretGuitar = new FiveFretGuitarColors
            {
                OpenFret   = IFretColorProvider.CircularOrange,
                GreenFret  = IFretColorProvider.AprilFoolsGreen,
                RedFret    = IFretColorProvider.AprilFoolsRed,
                YellowFret = IFretColorProvider.AprilFoolsYellow,
                BlueFret   = IFretColorProvider.AprilFoolsBlue,
                OrangeFret = IFretColorProvider.AprilFoolsPurple,

                OpenFretInner   = IFretColorProvider.CircularOrange,
                GreenFretInner  = IFretColorProvider.AprilFoolsGreen,
                RedFretInner    = IFretColorProvider.AprilFoolsRed,
                YellowFretInner = IFretColorProvider.AprilFoolsYellow,
                BlueFretInner   = IFretColorProvider.AprilFoolsBlue,
                OrangeFretInner = IFretColorProvider.AprilFoolsPurple,

                OpenNote   = IFretColorProvider.CircularOrange,
                GreenNote  = IFretColorProvider.AprilFoolsGreen,
                RedNote    = IFretColorProvider.AprilFoolsRed,
                YellowNote = IFretColorProvider.AprilFoolsYellow,
                BlueNote   = IFretColorProvider.AprilFoolsBlue,
                OrangeNote = IFretColorProvider.AprilFoolsPurple,

                OpenNoteStarPower   = IFretColorProvider.CircularStarpower,
                GreenNoteStarPower  = IFretColorProvider.CircularStarpower,
                RedNoteStarPower    = IFretColorProvider.CircularStarpower,
                YellowNoteStarPower = IFretColorProvider.CircularStarpower,
                BlueNoteStarPower   = IFretColorProvider.CircularStarpower,
                OrangeNoteStarPower = IFretColorProvider.CircularStarpower,
            },
            FourLaneDrums = new FourLaneDrumsColors
            {
                KickFret   = IFretColorProvider.AprilFoolsPurple,
                RedFret    = IFretColorProvider.AprilFoolsRed,
                YellowFret = IFretColorProvider.AprilFoolsYellow,
                BlueFret   = IFretColorProvider.AprilFoolsBlue,
                GreenFret  = IFretColorProvider.AprilFoolsGreen,

                KickFretInner   = IFretColorProvider.AprilFoolsPurple,
                RedFretInner    = IFretColorProvider.AprilFoolsRed,
                YellowFretInner = IFretColorProvider.AprilFoolsYellow,
                BlueFretInner   = IFretColorProvider.AprilFoolsBlue,
                GreenFretInner  = IFretColorProvider.AprilFoolsGreen,

                KickNote = IFretColorProvider.AprilFoolsPurple,

                RedDrum    = IFretColorProvider.AprilFoolsRed,
                YellowDrum = IFretColorProvider.AprilFoolsYellow,
                BlueDrum   = IFretColorProvider.AprilFoolsBlue,
                GreenDrum  = IFretColorProvider.AprilFoolsGreen,

                RedCymbal    = IFretColorProvider.AprilFoolsRed,
                YellowCymbal = IFretColorProvider.AprilFoolsYellow,
                BlueCymbal   = IFretColorProvider.AprilFoolsBlue,
                GreenCymbal  = IFretColorProvider.AprilFoolsGreen,

                KickStarpower = IFretColorProvider.CircularStarpower,

                RedDrumStarpower    = IFretColorProvider.CircularStarpower,
                YellowDrumStarpower = IFretColorProvider.CircularStarpower,
                BlueDrumStarpower   = IFretColorProvider.CircularStarpower,
                GreenDrumStarpower  = IFretColorProvider.CircularStarpower,

                RedCymbalStarpower    = IFretColorProvider.CircularStarpower,
                YellowCymbalStarpower = IFretColorProvider.CircularStarpower,
                BlueCymbalStarpower   = IFretColorProvider.CircularStarpower,
                GreenCymbalStarpower  = IFretColorProvider.CircularStarpower,
            },
            FiveLaneDrums = new FiveLaneDrumsColors
            {
                KickFret   = IFretColorProvider.CircularOrange,
                RedFret    = IFretColorProvider.AprilFoolsRed,
                YellowFret = IFretColorProvider.AprilFoolsYellow,
                BlueFret   = IFretColorProvider.AprilFoolsBlue,
                OrangeFret = IFretColorProvider.AprilFoolsPurple,
                GreenFret  = IFretColorProvider.AprilFoolsGreen,

                KickFretInner   = IFretColorProvider.CircularOrange,
                RedFretInner    = IFretColorProvider.AprilFoolsRed,
                YellowFretInner = IFretColorProvider.AprilFoolsYellow,
                BlueFretInner   = IFretColorProvider.AprilFoolsBlue,
                OrangeFretInner = IFretColorProvider.AprilFoolsPurple,
                GreenFretInner  = IFretColorProvider.AprilFoolsGreen,

                KickNote   = IFretColorProvider.CircularOrange,
                RedNote    = IFretColorProvider.AprilFoolsRed,
                YellowNote = IFretColorProvider.AprilFoolsYellow,
                BlueNote   = IFretColorProvider.AprilFoolsBlue,
                OrangeNote = IFretColorProvider.AprilFoolsPurple,
                GreenNote  = IFretColorProvider.AprilFoolsGreen,

                KickStarpower   = IFretColorProvider.CircularStarpower,
                RedStarpower    = IFretColorProvider.CircularStarpower,
                YellowStarpower = IFretColorProvider.CircularStarpower,
                BlueStarpower   = IFretColorProvider.CircularStarpower,
                OrangeStarpower = IFretColorProvider.CircularStarpower,
                GreenStarpower  = IFretColorProvider.CircularStarpower,
            }
        });

        public static readonly PresetContainer<ColorProfile>[] Defaults =
        {
            Default,
            CircularDefault,
            AprilFoolsDefault
        };

        public static bool IsDefault(in PresetContainer<ColorProfile> profile)
        {
            foreach (var def in Defaults)
            {
                if (def.Id == profile.Id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}