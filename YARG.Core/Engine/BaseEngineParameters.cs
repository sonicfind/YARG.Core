using System;
using System.IO;
using YARG.Core.Utility;

namespace YARG.Core.Engine
{
    public abstract class BaseEngineParameters
    {
        public HitWindowSettings HitWindow;

        public readonly int MaxMultiplier;

        public readonly float[] StarMultiplierThresholds;

        public double SongSpeed;

        protected BaseEngineParameters(HitWindowSettings hitWindow, int maxMultiplier, float[] starMultiplierThresholds)
        {
            HitWindow = hitWindow;
            MaxMultiplier = maxMultiplier;
            StarMultiplierThresholds = starMultiplierThresholds;
        }

        protected BaseEngineParameters(BinaryReader reader, int version)
        {
            HitWindow = new HitWindowSettings(reader);

            MaxMultiplier = reader.ReadInt32();

            // Read star multiplier thresholds
            StarMultiplierThresholds = new float[reader.ReadInt32()];
            for (int i = 0; i < StarMultiplierThresholds.Length; i++)
            {
                StarMultiplierThresholds[i] = reader.ReadSingle();
            }

            if (version >= 5)
            {
                SongSpeed = reader.ReadDouble();
            }
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            HitWindow.Serialize(writer);

            writer.Write(MaxMultiplier);

            // Write star multiplier thresholds
            writer.Write(StarMultiplierThresholds.Length);
            foreach (var f in StarMultiplierThresholds)
            {
                writer.Write(f);
            }

            writer.Write(SongSpeed);
        }
    }
}