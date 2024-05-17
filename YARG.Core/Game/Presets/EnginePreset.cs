using System;

namespace YARG.Core.Game
{
    public partial struct EngineConfig : IPresetConfig
    {
        private static readonly EngineConfig _defaultValues = new()
        {
            FiveFretGuitar = FiveFretGuitarPreset.Default,
            Drums = DrumsPreset.Default,
            Vocals = VocalsPreset.Default,
        };

        public FiveFretGuitarPreset FiveFretGuitar;
        public DrumsPreset Drums;
        public VocalsPreset Vocals;

        public readonly string Type => "EnginePreset";
    }
}