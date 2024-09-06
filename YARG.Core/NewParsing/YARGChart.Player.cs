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
                    }, Sync, profile, Settings.GetHopoThreshold(Sync.Tickrate), !Settings.ChordHopoCancellation);
                case GameMode.SixFretGuitar:
                    {
                        long hopoThreshold = Settings.GetHopoThreshold(Sync.Tickrate);
                        return profile.CurrentInstrument switch
                        {
                            Instrument.SixFretGuitar =>      NewLoading.Guitar.GuitarPlayer.Load(SixFretGuitar!,      Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.SixFretBass =>        NewLoading.Guitar.GuitarPlayer.Load(SixFretBass!,        Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.SixFretRhythm =>      NewLoading.Guitar.GuitarPlayer.Load(SixFretRhythm!,      Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.SixFretCoopGuitar =>  NewLoading.Guitar.GuitarPlayer.Load(SixFretCoopGuitar!,  Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),

                            Instrument.FiveFretGuitar =>     NewLoading.Guitar.GuitarPlayer.Load(FiveFretGuitar!,     Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.FiveFretBass =>       NewLoading.Guitar.GuitarPlayer.Load(FiveFretBass!,       Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.FiveFretRhythm =>     NewLoading.Guitar.GuitarPlayer.Load(FiveFretRhythm!,     Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.FiveFretCoopGuitar => NewLoading.Guitar.GuitarPlayer.Load(FiveFretCoopGuitar!, Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            Instrument.Keys =>               NewLoading.Guitar.GuitarPlayer.Load(Keys!,               Sync, profile, hopoThreshold, !Settings.ChordHopoCancellation),
                            _ => throw new InvalidOperationException(),
                        };
                    }
                case GameMode.FourLaneDrums:
                    if (FourLaneDrums != null)
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFourLane(FourLaneDrums, Sync, profile);
                    }
                    if (FiveLaneDrums != null)
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFourLane(FiveLaneDrums, Sync, profile);
                    }
                    throw new InvalidOperationException();
                case GameMode.FiveLaneDrums:
                    if (FiveLaneDrums != null)
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFiveLane(FiveLaneDrums, Sync, profile);
                    }
                    if (FourLaneDrums != null)
                    {
                        return NewLoading.Drums.DrumPlayer.LoadFiveLane(FourLaneDrums, Sync, profile);
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
