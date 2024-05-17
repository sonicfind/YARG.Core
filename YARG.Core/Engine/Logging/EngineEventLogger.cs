using System.Collections.Generic;
using System.IO;
using YARG.Core.Utility;

namespace YARG.Core.Engine.Logging
{
    public class EngineEventLogger
    {
        public IReadOnlyList<BaseEngineEvent> Events => _events;

        private readonly List<BaseEngineEvent> _events = new();

        public EngineEventLogger() { }

        public EngineEventLogger(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var engineEvent = LoadEvent(reader);
                if (engineEvent != null)
                {
                    _events.Add(engineEvent);
                }
            }
        }

        public void LogEvent(BaseEngineEvent engineEvent)
        {
            _events.Add(engineEvent);
        }

        public void Clear()
        {
            _events.Clear();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_events.Count);
            foreach (var engineEvent in _events)
            {
                engineEvent.Serialize(writer);
            }
        }

        private static BaseEngineEvent? LoadEvent(BinaryReader reader)
        {
            var type = (EngineEventType) reader.ReadInt32();
            return type switch
            {
                EngineEventType.Note => new NoteEngineEvent(reader),
                //EngineEventType.Sustain => new SustainEngineEvent(type, reader),
                EngineEventType.Timer => new TimerEngineEvent(reader),
                EngineEventType.Score => new ScoreEngineEvent(reader),
                EngineEventType.StarPower => new StarPowerEngineEvent(reader),
                _ => throw new System.Exception("Unsupprted Engine Event Type")
            };
        }
    }
}