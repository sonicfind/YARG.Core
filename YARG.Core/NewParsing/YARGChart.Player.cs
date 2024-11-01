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
                    return NewLoading.Guitar.GuitarPlayer.Load(profile.CurrentInstrument switch
                    {
                        Instrument.FiveFretGuitar =>     FiveFretGuitar!,
                        Instrument.FiveFretBass =>       FiveFretBass!,
                        Instrument.FiveFretRhythm =>     FiveFretRhythm!,
                        Instrument.FiveFretCoopGuitar => FiveFretCoopGuitar!,
                        Instrument.Keys =>               Keys!,
                        _ => throw new InvalidOperationException(),
                    }, Sync, profile, in Settings, 5);
                case GameMode.SixFretGuitar:
                    return profile.CurrentInstrument switch
                    {
                        Instrument.SixFretGuitar =>      NewLoading.Guitar.GuitarPlayer.Load(SixFretGuitar!,      Sync, profile, in Settings, 6),
                        Instrument.SixFretBass =>        NewLoading.Guitar.GuitarPlayer.Load(SixFretBass!,        Sync, profile, in Settings, 6),
                        Instrument.SixFretRhythm =>      NewLoading.Guitar.GuitarPlayer.Load(SixFretRhythm!,      Sync, profile, in Settings, 6),
                        Instrument.SixFretCoopGuitar =>  NewLoading.Guitar.GuitarPlayer.Load(SixFretCoopGuitar!,  Sync, profile, in Settings, 6),

                        Instrument.FiveFretGuitar =>     NewLoading.Guitar.GuitarPlayer.Load(FiveFretGuitar!,     Sync, profile, in Settings, 5),
                        Instrument.FiveFretBass =>       NewLoading.Guitar.GuitarPlayer.Load(FiveFretBass!,       Sync, profile, in Settings, 5),
                        Instrument.FiveFretRhythm =>     NewLoading.Guitar.GuitarPlayer.Load(FiveFretRhythm!,     Sync, profile, in Settings, 5),
                        Instrument.FiveFretCoopGuitar => NewLoading.Guitar.GuitarPlayer.Load(FiveFretCoopGuitar!, Sync, profile, in Settings, 5),
                        Instrument.Keys =>               NewLoading.Guitar.GuitarPlayer.Load(Keys!,               Sync, profile, in Settings, 5),
                        _ => throw new InvalidOperationException(),
                    };
                case GameMode.FourLaneDrums:
                    if (!FourLaneDrums.IsEmpty())
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFourLane(FourLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    if (!FiveLaneDrums.IsEmpty())
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFourLane(FiveLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    throw new InvalidOperationException();
                case GameMode.FiveLaneDrums:
                    if (!FiveLaneDrums.IsEmpty())
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFiveLane(FiveLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
                    }
                    if (!FourLaneDrums.IsEmpty())
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFiveLane(FourLaneDrums, Sync, profile, Settings.SustainCutoffThreshold);
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
