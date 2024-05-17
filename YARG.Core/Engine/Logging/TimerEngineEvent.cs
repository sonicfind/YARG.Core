using System;
using System.IO;

namespace YARG.Core.Engine.Logging
{
    public class TimerEngineEvent : BaseEngineEvent
    {
        public string TimerName = string.Empty;

        public double TimerValue;

        public bool TimerStarted;
        public bool TimerStopped;
        public bool TimerExpired;

        public TimerEngineEvent(double eventTime) : base(EngineEventType.Timer, eventTime)
        {
        }

        public TimerEngineEvent(BinaryReader reader)
            : base(reader)
        {
            TimerName = reader.ReadString();
            TimerValue = reader.ReadDouble();
            TimerStarted = reader.ReadBoolean();
            TimerStopped = reader.ReadBoolean();
            TimerExpired = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(TimerName);
            writer.Write(TimerValue);
            writer.Write(TimerStarted);
            writer.Write(TimerStopped);
            writer.Write(TimerExpired);
        }

        public override bool Equals(BaseEngineEvent? engineEvent)
        {
            if (!base.Equals(engineEvent))
            {
                return false;
            }

            if (engineEvent?.GetType() != typeof(TimerEngineEvent)) return false;

            var timerEvent = engineEvent as TimerEngineEvent;

            return timerEvent != null &&
                TimerName == timerEvent.TimerName &&
                Math.Abs(TimerValue - timerEvent.TimerValue) < double.Epsilon &&
                TimerStarted == timerEvent.TimerStarted &&
                TimerStopped == timerEvent.TimerStopped &&
                TimerExpired == timerEvent.TimerExpired;
        }
    }
}