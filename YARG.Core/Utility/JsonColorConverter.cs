using System;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;
using YARG.Core.Game;

namespace YARG.Core.Utility
{
    public class JsonColorConverter : JsonConverter<YARGColor>
    {
        public override void WriteJson(JsonWriter writer, YARGColor value, JsonSerializer serializer)
        {
            int argb = value.ToArgb();

            byte a = (byte) ((argb >> 24) & 0xFF);

            // Convert from ARGB to RGBA
            argb <<= 8;
            argb |= a;

            writer.WriteValue(argb.ToString("X8"));
        }

        public override YARGColor ReadJson(JsonReader reader, Type objectType, YARGColor existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return YARGColor.White;
            }

            var value = reader.Value.ToString();

            if (value.Length == 6)
            {
                value += "FF";
            } else if(value.Length != 8)
            {
                return YARGColor.White;
            }

            try
            {
                int rgba = int.Parse(value, NumberStyles.AllowHexSpecifier);

                var a = (byte) (rgba & 0xFF);

                // Convert from RGBA to ARGB
                rgba >>= 8;
                rgba |= a << 24;

                return YARGColor.FromArgb(rgba);
            }
            catch
            {
                return YARGColor.White;
            }

        }

        public override bool CanRead => true;
    }
}