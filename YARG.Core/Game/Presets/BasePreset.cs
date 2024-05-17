using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace YARG.Core.Game
{
    public interface IPresetConfig
    {
        public string Type { get; }
    }

    public struct Preset<TConfig>
        where TConfig : unmanaged, IPresetConfig
    {
        public readonly Guid Id;
        public string Name;
        public TConfig Data;

        public Preset(string name, bool defaultPreset, in TConfig data = default)
        {
            Name = name;
            Id = Id = defaultPreset
                ? GetGuidForBasePreset(name)
                : Guid.NewGuid();
            Data = data;
        }

        public Preset(string name, Guid id, in TConfig data)
        {
            Name = name;
            Id = id;
            Data = data;
        }

        private static Guid GetGuidForBasePreset(string name)
        {
            // Make sure default presets are consistent based on names.
            // This ensures that their GUIDs will be consistent (because they are constructed in code every time).
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
            return new Guid(hash);
        }
    }

    public readonly struct PresetInfo
    {
        public readonly string Name;
        public readonly Guid Id;
        public PresetInfo(string name, bool defaultPreset)
        {
            Name = name;
            Id = Id = defaultPreset
                ? GetGuidForBasePreset(name)
                : Guid.NewGuid();
        }

        [JsonConstructor]
        public PresetInfo(string name, Guid id)
        {
            Name= name;
            Id = id;
        }

        private static Guid GetGuidForBasePreset(string name)
        {
            // Make sure default presets are consistent based on names.
            // This ensures that their GUIDs will be consistent (because they are constructed in code every time).
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
            return new Guid(hash);
        }
    }

    public abstract class BasePreset
    {
        /// <summary>
        /// The display name of the preset.
        /// </summary>
        public string Name;

        /// <summary>
        /// The unique ID of the preset.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// The type of the preset in string form. This is only
        /// used for checking the type when importing a preset.
        /// </summary>
        public string? Type;

        /// <summary>
        /// Determines whether or not the preset should be modifiable in the settings.
        /// </summary>
        [JsonIgnore]
        public bool DefaultPreset;

        /// <summary>
        /// The path of the preset. This is only used to determine the path when it's in class form.
        /// </summary>
        [JsonIgnore]
        public string? Path;

        protected BasePreset(string name, bool defaultPreset)
        {
            Name = name;
            DefaultPreset = defaultPreset;

            Id = defaultPreset
                ? GetGuidForBasePreset(name)
                : Guid.NewGuid();
        }

        public abstract BasePreset CopyWithNewName(string name);

        private static Guid GetGuidForBasePreset(string name)
        {
            // Make sure default presets are consistent based on names.
            // This ensures that their GUIDs will be consistent (because they are constructed in code every time).
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
            return new Guid(hash);
        }
    }
}