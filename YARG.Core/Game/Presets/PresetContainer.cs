using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using YARG.Core.Utility;

namespace YARG.Core.Game
{
    public static class PresetJSON
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>
            {
                new JsonColorConverter(),
            }
        };
    }

    public struct PresetContainer<TPreset>
        where TPreset : unmanaged
    {
        public Guid Id;
        public string Name;
        public TPreset Config;

        public PresetContainer(string name, in TPreset preset)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
            Id = new Guid(hash);
            Name = name;
            Config = preset;
        }

        public readonly void Export(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
            using var writer = new StreamWriter(stream);
            var text = JsonConvert.SerializeObject(this, PresetJSON.Settings);
            writer.Write(text);
        }

        public static PresetContainer<TPreset> Import(string path)
        {
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PresetContainer<TPreset>>(file, PresetJSON.Settings);
        }
    }
}