using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Game;
using YARG.Core.NewLoading;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        public BasePlayer? LoadPlayer(YargProfile profile)
        {
            switch (profile.GameMode)
            {
                case GameMode.FiveFretGuitar:
                    return new NewLoading.Guitar.FiveFretPlayer(profile.CurrentInstrument switch
                    {
                        Instrument.FiveFretGuitar =>     FiveFretGuitar!,
                        Instrument.FiveFretBass =>       FiveFretBass!,
                        Instrument.FiveFretRhythm =>     FiveFretRhythm!,
                        Instrument.FiveFretCoopGuitar => FiveFretCoopGuitar!,
                        Instrument.Keys =>               Keys!,
                        _ => throw new InvalidOperationException(),
                    }, in Settings, Sync, profile);
                case GameMode.SixFretGuitar:
                    {
                        return profile.CurrentInstrument switch
                        {
                            Instrument.SixFretGuitar =>      new NewLoading.Guitar.SixFretPlayer(SixFretGuitar!,      in Settings, Sync, profile),
                            Instrument.SixFretBass =>        new NewLoading.Guitar.SixFretPlayer(SixFretBass!,        in Settings, Sync, profile),
                            Instrument.SixFretRhythm =>      new NewLoading.Guitar.SixFretPlayer(SixFretRhythm!,      in Settings, Sync, profile),
                            Instrument.SixFretCoopGuitar =>  new NewLoading.Guitar.SixFretPlayer(SixFretCoopGuitar!,  in Settings, Sync, profile),

                            Instrument.FiveFretGuitar =>     new NewLoading.Guitar.SixFretPlayer(FiveFretGuitar!,     in Settings, Sync, profile),
                            Instrument.FiveFretBass =>       new NewLoading.Guitar.SixFretPlayer(FiveFretBass!,       in Settings, Sync, profile),
                            Instrument.FiveFretRhythm =>     new NewLoading.Guitar.SixFretPlayer(FiveFretRhythm!,     in Settings, Sync, profile),
                            Instrument.FiveFretCoopGuitar => new NewLoading.Guitar.SixFretPlayer(FiveFretCoopGuitar!, in Settings, Sync, profile),
                            Instrument.Keys =>               new NewLoading.Guitar.SixFretPlayer(Keys!,               in Settings, Sync, profile),
                            _ => throw new InvalidOperationException(),
                        };
                    }
                case GameMode.FourLaneDrums:
                    if (FourLaneDrums != null)
                    {
                        return new NewLoading.FourLane.FourLanePlayer(FourLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    if (FiveLaneDrums != null)
                    {
                        return new NewLoading.FourLane.FourLanePlayer(FiveLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    throw new InvalidOperationException();
                case GameMode.FiveLaneDrums:
                    if (FiveLaneDrums != null)
                    {
                        return new NewLoading.FiveLane.FiveLanePlayer(FiveLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    if (FourLaneDrums != null)
                    {
                        return new NewLoading.FiveLane.FiveLanePlayer(FourLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    throw new InvalidOperationException();
                case GameMode.ProGuitar:
                    break;
                case GameMode.ProKeys:
                    break;
                case GameMode.Vocals:
                    break;
            }
            return null;
        }
    }
}
