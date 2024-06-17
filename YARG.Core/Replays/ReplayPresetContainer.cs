using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using YARG.Core.Extensions;
using YARG.Core.Game;
using YARG.Core.Utility;

namespace YARG.Core.Replays
{
    /// <summary>
    /// A container that stores the presets used in a replay, and allows for easy access of
    /// said presets. The container has separate versioning from the replay itself.
    /// </summary>
    public class ReplayPresetContainer : IBinarySerializable
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Converters =
            {
                new JsonColorConverter()
            }
        };

        private const int CONTAINER_VERSION = 0;

        private readonly List<PresetContainer<ColorProfile>> _colorProfiles = new();
        private readonly List<PresetContainer<CameraPreset>> _cameraPresets = new();

        /// <returns>
        /// The color profile if it's in this container, otherwise, <c>null</c>.
        /// </returns>
        public bool TryGetColorProfile(Guid guid, out PresetContainer<ColorProfile> profile)
        {
            foreach (var prof in _colorProfiles)
            {
                if (prof.Id == guid)
                {
                    profile = prof;
                    return true;
                }
            }
            profile = ColorProfile.Default;
            return false;
        }

        /// <summary>
        /// Stores the specified color profile into this container. If the color profile
        /// is a default one, nothing is stored.
        /// </summary>
        public void StoreColorProfile(PresetContainer<ColorProfile> colorProfile)
        {
            if (ColorProfile.IsDefault(in colorProfile))
            {
                return;
            }

            _colorProfiles.Add(colorProfile);
        }

        /// <returns>
        /// The camera preset if it's in this container, otherwise, <c>null</c>.
        /// </returns>
        public bool TryGetCameraPreset(Guid guid, out PresetContainer<CameraPreset> preset)
        {
            foreach (var camera in _cameraPresets)
            {
                if (camera.Id == guid)
                {
                    preset = camera;
                    return true;
                }
            }
            preset = CameraPreset.Default;
            return false;
        }

        /// <summary>
        /// Stores the specified camera preset into this container. If the camera preset
        /// is a default one, nothing is stored.
        /// </summary>
        public void StoreCameraPreset(PresetContainer<CameraPreset> cameraPreset)
        {
            if (CameraPreset.IsDefault(in cameraPreset))
            {
                return;
            }
            _cameraPresets.Add(cameraPreset);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(CONTAINER_VERSION);

            SerializeList(writer, _colorProfiles);
            SerializeList(writer, _cameraPresets);
        }

        public void Deserialize(BinaryReader reader, int version = 0)
        {
            // This container has separate versioning
            version = reader.ReadInt32();

            DeserializeList(reader, _colorProfiles);
            DeserializeList(reader, _cameraPresets);
        }

        private static void SerializeList<T>(BinaryWriter writer, List<PresetContainer<T>> presets)
            where T : struct
        {
            writer.Write(presets.Count);
            foreach (var preset in presets)
            {
                // Write preset
                var json = JsonConvert.SerializeObject(preset, _jsonSettings);
                writer.Write(json);
            }
        }

        private static void DeserializeList<T>(BinaryReader reader, List<PresetContainer<T>> presets)
            where T : struct
        {
            presets.Clear();
            int len = reader.ReadInt32();
            for (int i = 0; i < len; i++)
            {
                // Read key
                var guid = reader.ReadGuid();

                // Read preset
                var json = reader.ReadString();
                var preset = JsonConvert.DeserializeObject<PresetContainer<T>>(json, _jsonSettings)!;

                presets.Add(preset);
            }
        }
    }
}