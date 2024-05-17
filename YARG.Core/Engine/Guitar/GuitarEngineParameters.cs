using System.IO;

namespace YARG.Core.Engine.Guitar
{
    public class GuitarEngineParameters : BaseEngineParameters
    {
        public readonly double HopoLeniency;

        public readonly double StrumLeniency;
        public readonly double StrumLeniencySmall;

        public readonly double StarPowerWhammyBuffer;

        public readonly bool InfiniteFrontEnd;
        public readonly bool AntiGhosting;

        public GuitarEngineParameters(HitWindowSettings hitWindow, int maxMultiplier, float[] starMultiplierThresholds,
            double hopoLeniency, double strumLeniency, double strumLeniencySmall, double spWhammyBuffer,
            bool infiniteFrontEnd, bool antiGhosting)
            : base(hitWindow, maxMultiplier, starMultiplierThresholds)
        {
            HopoLeniency = hopoLeniency;

            StrumLeniency = strumLeniency;
            StrumLeniencySmall = strumLeniencySmall;

            StarPowerWhammyBuffer = spWhammyBuffer;

            InfiniteFrontEnd = infiniteFrontEnd;
            AntiGhosting = antiGhosting;
        }

        public GuitarEngineParameters(BinaryReader reader, int version)
            : base(reader, version)
        {
            HopoLeniency = reader.ReadDouble();

            StrumLeniency = reader.ReadDouble();
            StrumLeniencySmall = reader.ReadDouble();

            StarPowerWhammyBuffer = reader.ReadDouble();

            InfiniteFrontEnd = reader.ReadBoolean();
            AntiGhosting = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(HopoLeniency);

            writer.Write(StrumLeniency);
            writer.Write(StrumLeniencySmall);

            writer.Write(StarPowerWhammyBuffer);

            writer.Write(InfiniteFrontEnd);
            writer.Write(AntiGhosting);
        }
    }
}