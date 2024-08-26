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
                    }, Settings.GetHopoThreshold(Sync.Tickrate), !Settings.ChordHopoCancellation, Sync, profile);
                case GameMode.SixFretGuitar:
                    {
                        long hopoThreshold = Settings.GetHopoThreshold(Sync.Tickrate);
                        bool allowHopoAfterChord = !Settings.ChordHopoCancellation;
                        return profile.CurrentInstrument switch
                        {
                            Instrument.SixFretGuitar =>      new NewLoading.Guitar.SixFretPlayer(SixFretGuitar!,      hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.SixFretBass =>        new NewLoading.Guitar.SixFretPlayer(SixFretBass!,        hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.SixFretRhythm =>      new NewLoading.Guitar.SixFretPlayer(SixFretRhythm!,      hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.SixFretCoopGuitar =>  new NewLoading.Guitar.SixFretPlayer(SixFretCoopGuitar!,  hopoThreshold, allowHopoAfterChord, Sync, profile),

                            Instrument.FiveFretGuitar =>     new NewLoading.Guitar.SixFretPlayer(FiveFretGuitar!,     hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.FiveFretBass =>       new NewLoading.Guitar.SixFretPlayer(FiveFretBass!,       hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.FiveFretRhythm =>     new NewLoading.Guitar.SixFretPlayer(FiveFretRhythm!,     hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.FiveFretCoopGuitar => new NewLoading.Guitar.SixFretPlayer(FiveFretCoopGuitar!, hopoThreshold, allowHopoAfterChord, Sync, profile),
                            Instrument.Keys =>               new NewLoading.Guitar.SixFretPlayer(Keys!,               hopoThreshold, allowHopoAfterChord, Sync, profile),
                            _ => throw new InvalidOperationException(),
                        };
                    }
                case GameMode.FourLaneDrums:
                    if (FourLaneDrums != null)
                    {
                        return new NewLoading.FourLane.FourLanePlayer(FourLaneDrums, Sync, profile);
                    }
                    if (FiveLaneDrums != null)
                    {
                        return new NewLoading.FourLane.FourLanePlayer(FiveLaneDrums, Sync, profile);
                    }
                    throw new InvalidOperationException();
                case GameMode.FiveLaneDrums:
                    if (FiveLaneDrums != null)
                    {
                        return new NewLoading.FiveLane.FiveLanePlayer(FiveLaneDrums, Sync, profile);
                    }
                    if (FourLaneDrums != null)
                    {
                        return new NewLoading.FiveLane.FiveLanePlayer(FourLaneDrums, Sync, profile);
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
