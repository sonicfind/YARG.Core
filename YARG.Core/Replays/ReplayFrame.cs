using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using YARG.Core.Engine;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Guitar;
using YARG.Core.Engine.Logging;
using YARG.Core.Engine.Vocals;
using YARG.Core.Input;
using YARG.Core.Utility;

namespace YARG.Core.Replays
{
    public class ReplayFrame
    {
        public readonly ReplayPlayerInfo     PlayerInfo;
        public readonly BaseEngineParameters EngineParameters;
        public readonly BaseStats            Stats;
        public readonly GameInput[]          Inputs;
        public readonly EngineEventLogger    EventLog;

        public int InputCount => Inputs.Length;

        public ReplayFrame(ReplayPlayerInfo info, BaseEngineParameters param, BaseStats stats, GameInput[] inputs, EngineEventLogger logger)
        {
            PlayerInfo = info;
            Stats = stats;
            EngineParameters = param;
            Inputs = inputs;
            EventLog = logger;
        }

        public ReplayFrame(BinaryReader reader, int version)
        {
            PlayerInfo = new ReplayPlayerInfo(reader);

            switch (PlayerInfo.Profile.CurrentInstrument.ToGameMode())
            {
                case GameMode.FiveFretGuitar:
                case GameMode.SixFretGuitar:
                    EngineParameters = new GuitarEngineParameters(reader, version);
                    Stats = new GuitarStats(reader);
                    break;
                case GameMode.FourLaneDrums:
                case GameMode.FiveLaneDrums:
                    EngineParameters = new DrumsEngineParameters(reader, version);
                    Stats = new DrumsStats(reader);
                    break;
                case GameMode.Vocals:
                    EngineParameters = new VocalsEngineParameters(reader, version);
                    Stats = new VocalsStats(reader);
                    break;
                default:
                    throw new InvalidOperationException("Stat creation not implemented.");
            }

            int count = reader.ReadInt32();
            Inputs = new GameInput[count];
            for (int i = 0; i < count; i++)
            {
                double time = reader.ReadDouble();
                int action = reader.ReadInt32();
                int value = reader.ReadInt32();

                Inputs[i] = new GameInput(time, action, value);
            }

            EventLog = new EngineEventLogger(reader);
        }

        public void Serialize(BinaryWriter writer)
        {
            PlayerInfo.Serialize(writer);
            EngineParameters.Serialize(writer);
            Stats.Serialize(writer);

            writer.Write(InputCount);
            for (int i = 0; i < InputCount; i++)
            {
                writer.Write(Inputs[i].Time);
                writer.Write(Inputs[i].Action);
                writer.Write(Inputs[i].Integer);
            }
            
            EventLog.Serialize(writer);
        }
    }
}