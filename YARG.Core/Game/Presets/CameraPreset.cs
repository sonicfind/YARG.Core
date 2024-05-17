using Newtonsoft.Json;
using System;

namespace YARG.Core.Game
{
    public struct CameraConfig
    {
        public float FieldOfView;

        public float PositionY;
        public float PositionZ;
        public float Rotation;

        public float FadeLength;

        public float CurveFactor;
    }

    public partial struct CameraPreset : IPreset<CameraPreset>
    {
        private readonly PresetInfo _info;
        public float FieldOfView;

        public float PositionY;
        public float PositionZ;
        public float Rotation;

        public float FadeLength;

        public float CurveFactor;

        public readonly string Type => "CameraPreset";
        public readonly string Name => _info.Name;
        public readonly Guid Id => _info.Id;

        public CameraPreset(string name)
        {
            _info = new PresetInfo(name, true);
            FieldOfView = default;
            PositionY = default;
            PositionZ = default;
            Rotation = default;
            FadeLength = default;
            CurveFactor = default;
        }

        private CameraPreset(string name, in CameraPreset preset)
        {
            _info = new PresetInfo(name, false);
            FieldOfView = preset.FieldOfView;
            PositionY = preset.PositionY;
            PositionZ = preset.PositionZ;
            Rotation = preset.Rotation;
            FadeLength = preset.FadeLength;
            CurveFactor = preset.CurveFactor;
        }

        public readonly CameraPreset Copy(string name)
        {
            return new CameraPreset(name, in this);
        }
    }

    public class JSONCameraPresetConverter : JsonConverter<CameraPreset>
    {
        public override CameraPreset ReadJson(JsonReader reader, Type objectType, CameraPreset existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string name = serializer.Deserialize<string>(reader);
            Guid id = serializer.Deserialize<Guid>(reader);


        }

        public override void WriteJson(JsonWriter writer, CameraPreset value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}