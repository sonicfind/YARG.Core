using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace YARG.Core.Game
{
    public static class PresetGuid
    {
        public static Guid GetGuidForBasePreset(string name)
        {
            // Make sure default presets are consistent based on names.
            // This ensures that their GUIDs will be consistent (because they are constructed in code every time).
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
            return new Guid(hash);
        }
    }

    public interface IPreset<TPreset>
        where TPreset : struct, IPreset<TPreset>
    {
        public string Name { get; set; }
        public Guid Id { get; }
        public string Type { get; }
        TPreset Copy(string name);
    }
}