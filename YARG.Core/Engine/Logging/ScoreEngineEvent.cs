using System.IO;

namespace YARG.Core.Engine.Logging
{
    public class ScoreEngineEvent : BaseEngineEvent
    {
        public int Score;

        public ScoreEngineEvent(double eventTime) : base(EngineEventType.Score, eventTime)
        {
        }

        public ScoreEngineEvent(BinaryReader reader)
            : base(reader)
        {
            Score = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(Score);
        }

        
    }
}