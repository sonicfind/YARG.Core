using System;
using System.Collections.Generic;
using System.Linq;

namespace YARG.Core.Game
{
    public partial struct EnginePreset
    {
        public static readonly PresetContainer<EnginePreset> Default = new("Default", new EnginePreset()
        {
            FiveFretGuitar = FiveFretGuitarPreset.Default,
            Drums = DrumsPreset.Default,
            Vocals = VocalsPreset.Default,
        });

        public static readonly PresetContainer<EnginePreset> Casual = new("Casual", new EnginePreset()
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
        });

        public static PresetContainer<EnginePreset> Precision = new("Precision", new EnginePreset()
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
        });

        public static readonly PresetContainer<EnginePreset>[] Defaults =
        {
            Default,
            Casual,
            Precision
        };

        public static bool IsDefault(in PresetContainer<EnginePreset> engine)
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