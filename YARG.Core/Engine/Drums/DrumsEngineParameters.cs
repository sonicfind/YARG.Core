using System.IO;

namespace YARG.Core.Engine.Drums
{
    public class DrumsEngineParameters : BaseEngineParameters
    {
        public enum DrumMode : byte
        {
            NonProFourLane,
            ProFourLane,
            FiveLane
        }

        /// <summary>
        /// What mode the inputs should be processed in.
        /// </summary>
        public readonly DrumMode Mode;

        public DrumsEngineParameters(HitWindowSettings hitWindow, int maxMultiplier, float[] starMultiplierThresholds,
            DrumMode mode)
            : base(hitWindow, maxMultiplier, starMultiplierThresholds)
        {
            Mode = mode;
        }

        public DrumsEngineParameters(BinaryReader reader, int version)
            : base(reader, version)
        {
            Mode = (DrumMode) reader.ReadByte();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte) Mode);
        }
    }
}