using System;
using System.Collections.Generic;
using System.Linq;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
        public static EnginePreset Default = new("Default");

        public static EnginePreset Casual = new("Casual")
        {
            FiveFretGuitar = new FiveFretGuitarPreset
            {
                AntiGhosting = false,
                InfiniteFrontEnd = true,
                StrumLeniency = 0.06,
                StrumLeniencySmall = 0.03
            },
            Drums = DrumsPreset.Default,
            Vocals = new VocalsPreset
            {
                WindowSizeE = 2.2,
                WindowSizeM = 1.8,
                WindowSizeH = 1.4,
                WindowSizeX = 1,
            },
        };

        public static EnginePreset Precision = new("Precision")
        {
            FiveFretGuitar = new FiveFretGuitarPreset
            {
                StrumLeniency = 0.04,
                StrumLeniencySmall = 0.02,
                HitWindow =
                {
                    MaxWindow = 0.13,
                    MinWindow = 0.04,
                    IsDynamic = true,
                }
            },
            Drums = new DrumsPreset
            {
                HitWindow =
                {
                    MaxWindow = 0.13,
                    MinWindow = 0.04,
                    IsDynamic = true,
                }
            },
            Vocals = new VocalsPreset
            {
                WindowSizeE = 1.2,
                WindowSizeM = 1,
                WindowSizeH = 0.8,
                WindowSizeX = 0.6
            }
        };

        public static readonly EnginePreset[] Defaults =
        {
            Default,
            Casual,
            Precision
        };

        public static bool IsDefault(in EnginePreset engine)
        {
            foreach (var def in Defaults)
            {
                if (def.Id == engine.Id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}