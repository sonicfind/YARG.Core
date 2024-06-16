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
            public Color GetFretColor(int index);
            public Color GetFretInnerColor(int index);
            public Color GetParticleColor(int index);
        }

        public FiveFretGuitarColors FiveFretGuitar;
        public FourLaneDrumsColors FourLaneDrums;
        public FiveLaneDrumsColors FiveLaneDrums;
    }
}