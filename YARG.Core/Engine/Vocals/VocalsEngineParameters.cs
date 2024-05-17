using System.IO;

namespace YARG.Core.Engine.Vocals
{
    public class VocalsEngineParameters : BaseEngineParameters
    {
        /// <summary>
        /// The percent of ticks that have to be correct in a phrase for it to count as a hit.
        /// </summary>
        public readonly double PhraseHitPercent;

        /// <summary>
        /// How often the vocals give a pitch reading (approximately).
        /// </summary>
        public readonly double ApproximateVocalFps;

        /// <summary>
        /// Whether or not the player can sing to activate starpower.
        /// </summary>
        public readonly bool SingToActivateStarPower;

        public VocalsEngineParameters(HitWindowSettings hitWindow, int maxMultiplier, float[] starMultiplierThresholds,
            double phraseHitPercent, bool singToActivateStarPower, double approximateVocalFps)
            : base(hitWindow, maxMultiplier, starMultiplierThresholds)
        {
            PhraseHitPercent = phraseHitPercent;
            ApproximateVocalFps = approximateVocalFps;
            SingToActivateStarPower = singToActivateStarPower;
        }

        public VocalsEngineParameters(BinaryReader reader, int version)
            : base(reader, version)
        {
            PhraseHitPercent = reader.ReadDouble();
            ApproximateVocalFps = reader.ReadDouble();
            SingToActivateStarPower = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(PhraseHitPercent);
            writer.Write(ApproximateVocalFps);
            writer.Write(SingToActivateStarPower);
        }
    }
}