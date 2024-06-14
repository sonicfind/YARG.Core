using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace YARG.Core.Game
{
    public partial struct ColorProfile : IPreset<ColorProfile>
    {
        public const int COLOR_PROFILE_VERSION = 1;

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

        public string Name { get; set; }
        public readonly Guid Id { get; }

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
            return new ColorProfile(in fivefret, in fourlane, in fivelane, name, Guid.NewGuid());
        }

        public ColorProfile(string name)
        {
            Name = name;
            Id = PresetGuid.GetGuidForBasePreset(name);
            FiveFretGuitar = FiveFretGuitarColors.Default;
            FourLaneDrums = FourLaneDrumsColors.Default;
            FiveLaneDrums = FiveLaneDrumsColors.Default;
        }

        [JsonConstructor]
        public ColorProfile(in FiveFretGuitarColors fivefret, in FourLaneDrumsColors fourlane, in FiveLaneDrumsColors fivelane, string name, Guid id)
        {
            Name = name;
            Id = id;
            FiveFretGuitar = fivefret;
            FourLaneDrums = fourlane;
            FiveLaneDrums = fivelane;
        }

        public readonly ColorProfile Copy(string name)
        {
            return new ColorProfile(in FiveFretGuitar, in FourLaneDrums, in FiveLaneDrums, name, Guid.NewGuid());
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