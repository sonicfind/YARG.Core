using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Engine.Drums;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
        /// <summary>
        /// The engine preset for four and five lane drums. These two game modes
        /// use the same engine, so there's no point in splitting them up.
        /// </summary>
        public struct DrumsPreset
        {
            public static readonly DrumsPreset Default = new()
            {
                HitWindow = new()
                {
                    MaxWindow = 0.14,
                    MinWindow = 0.14,
                    IsDynamic = false,
                    FrontToBackRatio = 1.0
                }
            };

            public HitWindowPreset HitWindow;

            public readonly DrumsEngineParameters Create(float[] starMultiplierThresholds, DrumsEngineParameters.DrumMode mode)
            {
                var hitWindow = HitWindow.Create();
                return new DrumsEngineParameters(
                    hitWindow,
                    DEFAULT_MAX_MULTIPLIER,
                    starMultiplierThresholds,
                    mode);
            }
        }
    }
}
