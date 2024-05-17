using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using YARG.Core.Utility;

namespace YARG.Core.Game
{
    public struct ColorConfig : IPresetConfig
    {
        private const int COLOR_PROFILE_VERSION = 1;
        /// <summary>
        /// Interface that has methods that allows for generic fret color retrieval.
        /// Not all instruments have frets, so it's an interface.
        /// </summary>
        public interface IFretColorProvider
        {
            public Color GetFretColor(int index);
            public Color GetFretInnerColor(int index);
            public Color GetParticleColor(int index);
        }

        public FiveFretGuitarColors FiveFretGuitar;
        public FourLaneDrumsColors FourLaneDrums;
        public FiveLaneDrumsColors FiveLaneDrums;

        public readonly string Type => "ColorProfile";

        private ColorConfig(BinaryReader reader)
        {
            FiveFretGuitar = new FiveFretGuitarColors(reader);
            FourLaneDrums = new FourLaneDrumsColors(reader);
            FiveLaneDrums = new FiveLaneDrumsColors(reader);
        }

        public static Preset<ColorConfig> Create(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            string name = reader.ReadString();
            var config = new ColorConfig(reader);
            return new Preset<ColorConfig>(name, false, config);
        }
    }

    public partial struct ColorProfile : IPreset<ColorProfile>
    {
        private const int COLOR_PROFILE_VERSION = 1;

        /// <summary>
        /// Interface that has methods that allows for generic fret color retrieval.
        /// Not all instruments have frets, so it's an interface.
        /// </summary>
        public interface IFretColorProvider
        {
            public Color GetFretColor(int index);
            public Color GetFretInnerColor(int index);
            public Color GetParticleColor(int index);
        }

        private readonly PresetInfo _info;
        public FiveFretGuitarColors FiveFretGuitar;
        public FourLaneDrumsColors  FourLaneDrums;
        public FiveLaneDrumsColors  FiveLaneDrums;

        public readonly string Type => "ColorProfile";
        public readonly string Name => _info.Name;
        public readonly Guid Id => _info.Id;

        // Have to do this instead of a constructor because
        // the order the data is saved means you can't send the "name" to the base type.
        // In other words, we fucked up.
        public static ColorProfile Create(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            string name = reader.ReadString();
            var fivefret = new FiveFretGuitarColors(reader);
            var fourlane = new FourLaneDrumsColors(reader);
            var fivelane = new FiveLaneDrumsColors(reader);
            return new ColorProfile(name, in fivefret, in fourlane, in fivelane);
        }

        public ColorProfile(string name)
        {
            _info = new PresetInfo(name, true);
            FiveFretGuitar = FiveFretGuitarColors.Default;
            FourLaneDrums = FourLaneDrumsColors.Default;
            FiveLaneDrums = FiveLaneDrumsColors.Default;
        }

        private ColorProfile(string name, in FiveFretGuitarColors fivefret, in FourLaneDrumsColors fourlane, in FiveLaneDrumsColors fivelane)
        {
            _info = new PresetInfo(name, false);
            FiveFretGuitar = fivefret;
            FourLaneDrums = fourlane;
            FiveLaneDrums = fivelane;
        }

        public readonly ColorProfile Copy(string name)
        {
            return new ColorProfile(name, in FiveFretGuitar, in FourLaneDrums, in FiveLaneDrums);
        }

        public readonly void Serialize(BinaryWriter writer)
        {
            writer.Write(COLOR_PROFILE_VERSION);
            writer.Write(Name);

            FiveFretGuitar.Serialize(writer);
            FourLaneDrums.Serialize(writer);
            FiveLaneDrums.Serialize(writer);
        }
    }
}