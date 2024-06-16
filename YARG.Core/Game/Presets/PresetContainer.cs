using System;
using System.Security.Cryptography;
using System.Text;

namespace YARG.Core.Game
{
    public struct PresetContainer<TPreset>
        where TPreset : struct
    {
        public readonly Guid Id;
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

        public PresetContainer(Guid id, string name, in TPreset preset)
        {
            Id = id;
            Name = name;
            Config = preset;
        }
    }
}