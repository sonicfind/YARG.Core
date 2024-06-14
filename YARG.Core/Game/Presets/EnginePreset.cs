using Newtonsoft.Json;
using System;

namespace YARG.Core.Game
{
    public partial struct EnginePreset : IPreset<EnginePreset>
    {
        public FiveFretGuitarPreset FiveFretGuitar;
        public DrumsPreset          Drums;
        public VocalsPreset         Vocals;

        public string Name { get; set; }
        public readonly Guid Id { get; }

        public EnginePreset(string name)
        {
            Name = name;
            Id = PresetGuid.GetGuidForBasePreset(name);
            FiveFretGuitar = FiveFretGuitarPreset.Default;
            Drums = DrumsPreset.Default;
            Vocals = VocalsPreset.Default;
        }

        [JsonConstructor]
        public EnginePreset(in FiveFretGuitarPreset fivefret, in DrumsPreset drums, in VocalsPreset vocals, string name, Guid id)
        {
            Name = name;
            Id = id;
            FiveFretGuitar = fivefret;
            Drums = drums;
            Vocals = vocals;
        }

        public readonly EnginePreset Copy(string name)
        {
            return new EnginePreset(in FiveFretGuitar, in Drums, in Vocals, name, Guid.NewGuid());
        }
    }
}