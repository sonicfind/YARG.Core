using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Engine.Vocals;
using YARG.Core.Engine;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
        /// <summary>
        /// The engine preset for vocals/harmonies.
        /// </summary>
        public struct VocalsPreset
        {
            public static readonly VocalsPreset Default = new()
            {
                WindowSizeE = 1.7,
                WindowSizeM = 1.4,
                WindowSizeH = 1.1,
                WindowSizeX = 0.8,
                HitPercentE = 0.325,
                HitPercentM = 0.400,
                HitPercentH = 0.450,
                HitPercentX = 0.575,
            };

            // Hit window is in semitones (max. difference between correct pitch and sung pitch).
            public double WindowSizeE;
            public double WindowSizeM;
            public double WindowSizeH;
            public double WindowSizeX;

            // These percentages may seem low, but accounting for delay,
            // plosives not being detected, etc., it's pretty good.
            public double HitPercentE;
            public double HitPercentM;
            public double HitPercentH;
            public double HitPercentX;

            public readonly VocalsEngineParameters Create(float[] starMultiplierThresholds, Difficulty difficulty, float updatesPerSecond)
            {
                // Hit window is in semitones (max. difference between correct pitch and sung pitch).
                double windowSize = difficulty switch
                {
                    Difficulty.Easy => WindowSizeE,
                    Difficulty.Medium => WindowSizeM,
                    Difficulty.Hard => WindowSizeH,
                    Difficulty.Expert => WindowSizeX,
                    _ => throw new InvalidOperationException("Unreachable")
                };

                double hitPercent = difficulty switch
                {
                    Difficulty.Easy => HitPercentE,
                    Difficulty.Medium => HitPercentM,
                    Difficulty.Hard => HitPercentH,
                    Difficulty.Expert => HitPercentX,
                    _ => throw new InvalidOperationException("Unreachable")
                };

                int pointsPerPhrase = difficulty switch
                {
                    Difficulty.Easy => 400,
                    Difficulty.Medium => 800,
                    Difficulty.Hard => 1600,
                    Difficulty.Expert => 2000,
                    _ => throw new InvalidOperationException("Unreachable")
                };

                var hitWindow = new HitWindowSettings(windowSize, 0.03, 1, false);
                return new VocalsEngineParameters(
                    hitWindow,
                    DEFAULT_MAX_MULTIPLIER,
                    starMultiplierThresholds,
                    hitPercent,
                    true,
                    updatesPerSecond,
                    pointsPerPhrase);
            }
        }
    }
}
