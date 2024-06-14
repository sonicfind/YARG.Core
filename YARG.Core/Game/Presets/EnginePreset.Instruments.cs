using System;
using YARG.Core.Engine;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Guitar;
using YARG.Core.Engine.Vocals;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
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