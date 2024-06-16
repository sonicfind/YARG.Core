using Newtonsoft.Json;
using System;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Guitar;
using YARG.Core.Engine.Vocals;
using YARG.Core.Engine;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
        public enum Type
        {
            FiveFretGuitarPreset,
            DrumsPreset,
            VocalsPreset,
        }

        public const double DEFAULT_WHAMMY_BUFFER = 0.25;

        public const int DEFAULT_MAX_MULTIPLIER = 4;
        public const int BASS_MAX_MULTIPLIER = 6;

        /// <summary>
        /// A preset for a hit window. This should
        /// be used within each engine preset class.
        /// </summary>
        public struct HitWindowPreset
        {
            public double MaxWindow;
            public double MinWindow;

            public bool IsDynamic;

            public double FrontToBackRatio;

            public readonly HitWindowSettings Create()
            {
                return new HitWindowSettings(MaxWindow, MinWindow, FrontToBackRatio, IsDynamic);
            }
        }

        public FiveFretGuitarPreset FiveFretGuitar;
        public DrumsPreset          Drums;
        public VocalsPreset         Vocals;
    }
}