using System.IO;

namespace YARG.Core.Engine.Logging
{
    public class StarPowerEngineEvent : BaseEngineEvent
    {
        public bool IsActive;

        public StarPowerEngineEvent(double eventTime) : base(EngineEventType.StarPower, eventTime)
        {
        }

        public StarPowerEngineEvent(BinaryReader reader)
            : base(reader)
        {
            IsActive = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(IsActive);
        }
    }
}