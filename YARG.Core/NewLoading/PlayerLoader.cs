using System.Collections.Generic;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct InstrumentSelection
    {
        public Instrument Instrument;
        public Difficulty Difficulty;
        public Modifier Modifiers;
    }

    public static class PlayerLoader
    {
        public static List<YargPlayer> CreatePlayers(YARGChart chart, List<InstrumentSelection> selections)
        {
            var endTime = chart.GetEndTime();
            var players = new List<YargPlayer>();
            foreach (var selection in selections)
            {
                switch (selection.Instrument)
                {
                    case Instrument.FiveFretGuitar:     players.Add(FiveFretPlayer.Create(chart, chart.FiveFretGuitar,     in endTime, in selection)); break;
                    case Instrument.FiveFretBass:       players.Add(FiveFretPlayer.Create(chart, chart.FiveFretBass,       in endTime, in selection)); break;
                    case Instrument.FiveFretRhythm:     players.Add(FiveFretPlayer.Create(chart, chart.FiveFretRhythm,     in endTime, in selection)); break;
                    case Instrument.FiveFretCoopGuitar: players.Add(FiveFretPlayer.Create(chart, chart.FiveFretCoopGuitar, in endTime, in selection)); break;
                    case Instrument.Keys:               players.Add(FiveFretPlayer.Create(chart, chart.Keys,               in endTime, in selection)); break;
                }
            }
            return players;
        }
    }
}