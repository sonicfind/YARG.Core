using System;
using System.Collections.Generic;
using System.Linq;

namespace YARG.Core.Game
{
    public partial struct EngineConfig
    {
        public static Preset<EngineConfig> Default = new("Default", true, _defaultValues);

        public static Preset<EngineConfig> Casual = new("Casual", true,
            new EngineConfig()
            {
                FiveFretGuitar =
                {
                    AntiGhosting = false,
                    InfiniteFrontEnd = true,
                    StrumLeniency = 0.06,
                    StrumLeniencySmall = 0.03
                },
                Drums = DrumsPreset.Default,
                Vocals =
                {
                    WindowSizeE = 2.2,
                    WindowSizeM = 1.8,
                    WindowSizeH = 1.4,
                    WindowSizeX = 1
                }
            }
        );

        public static Preset<EngineConfig> Precision = new("Precision", true,
            new EngineConfig()
            {
                FiveFretGuitar =
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
                Drums =
                {
                    HitWindow =
                    {
                        MaxWindow = 0.13,
                        MinWindow = 0.04,
                        IsDynamic = true,
                    }
                },
                Vocals =
                {
                    WindowSizeE = 1.2,
                    WindowSizeM = 1,
                    WindowSizeH = 0.8,
                    WindowSizeX = 0.6
                }
            }
        );

        public static readonly List<Preset<EngineConfig>> Defaults = new()
        {
            Default,
            Casual,
            Precision
        };

        private static readonly HashSet<Guid> _defaultIDs;

        static EngineConfig()
        {
            _defaultIDs = new();
            foreach (var def in Defaults)
            {
                _defaultIDs.Add(def.Id);
            }
        }

        public static bool IsDefault(in Preset<EngineConfig> profile)
        {
            return _defaultIDs.Contains(profile.Id);
        }
    }
}