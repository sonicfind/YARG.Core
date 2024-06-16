using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Engine.Guitar;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
        /// <summary>
        /// The engine preset for five fret guitar.
        /// </summary>
        public struct FiveFretGuitarPreset
        {
            public static readonly FiveFretGuitarPreset Default = new()
            {
                AntiGhosting = true,
                InfiniteFrontEnd = false,
                HopoLeniency = 0.08,
                StrumLeniency = 0.05,
                StrumLeniencySmall = 0.025,
                HitWindow = new()
                {
                    MaxWindow = 0.14,
                    MinWindow = 0.14,
                    IsDynamic = false,
                    FrontToBackRatio = 1.0
                }
            };

            public bool AntiGhosting;
            public bool InfiniteFrontEnd;

            public double HopoLeniency;

            public double StrumLeniency;
            public double StrumLeniencySmall;

            public HitWindowPreset HitWindow;

            public readonly GuitarEngineParameters Create(float[] starMultiplierThresholds, bool isBass)
            {
                var hitWindow = HitWindow.Create();
                return new GuitarEngineParameters(
                    hitWindow,
                    isBass ? BASS_MAX_MULTIPLIER : DEFAULT_MAX_MULTIPLIER,
                    starMultiplierThresholds,
                    HopoLeniency,
                    StrumLeniency,
                    StrumLeniencySmall,
                    DEFAULT_WHAMMY_BUFFER,
                    InfiniteFrontEnd,
                    AntiGhosting);
            }
        }
    }
}
