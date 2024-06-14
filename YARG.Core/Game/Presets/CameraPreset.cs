using Newtonsoft.Json;
using System;

namespace YARG.Core.Game
{
    public partial struct CameraPreset : IPreset<CameraPreset>
    {
        public float FieldOfView;

        public float PositionY;
        public float PositionZ;
        public float Rotation;

        public float FadeLength;

        public float CurveFactor;

        public string Name { get; set; }
        public readonly Guid Id { get; }

        public CameraPreset(string name)
        {
            Name = name;
            Id = PresetGuid.GetGuidForBasePreset(name);
            FieldOfView = default;
            PositionY = default;
            PositionZ = default;
            Rotation = default;
            FadeLength = default;
            CurveFactor = default;
        }

        private CameraPreset(string name, in CameraPreset preset)
        {
            Name = name;
            Id = Guid.NewGuid();
            FieldOfView = preset.FieldOfView;
            PositionY = preset.PositionY;
            PositionZ = preset.PositionZ;
            Rotation = preset.Rotation;
            FadeLength = preset.FadeLength;
            CurveFactor = preset.CurveFactor;
        }

        [JsonConstructor]
        public CameraPreset(float fov, float posY, float posZ, float rot, float fade, float curve, string name, Guid id)
        {
            Name = name;
            Id = id;
            FieldOfView = fov;
            PositionY = posY;
            PositionZ = posZ;
            Rotation = rot;
            FadeLength = fade;
            CurveFactor = curve;
        }

        public readonly CameraPreset Copy(string name)
        {
            return new CameraPreset(name, in this);
        }
    }
}