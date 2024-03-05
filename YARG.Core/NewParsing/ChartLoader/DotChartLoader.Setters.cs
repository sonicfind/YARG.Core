using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public static partial class YARGDotChartLoader
    {
        private static bool Set(ref GuitarNote2<FiveFret> note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note.Frets.Green =  DualTime.Truncate(length); break;
                case 1: note.Frets.Red =    DualTime.Truncate(length); break;
                case 2: note.Frets.Yellow = DualTime.Truncate(length); break;
                case 3: note.Frets.Blue =   DualTime.Truncate(length); break;
                case 4: note.Frets.Orange = DualTime.Truncate(length); break;
                case 5:
                    if (note.State == GuitarState.Natural)
                    {
                        note.State = GuitarState.Forced;
                    }
                    break;
                case 6: note.State = GuitarState.Tap; break;
                case 7: note.Frets.Open = DualTime.Truncate(length); break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(ref GuitarNote2<SixFret> note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note.Frets.White1 = DualTime.Truncate(length); break;
                case 1: note.Frets.White2 = DualTime.Truncate(length); break;
                case 2: note.Frets.White3 = DualTime.Truncate(length); break;
                case 3: note.Frets.Black1 = DualTime.Truncate(length); break;
                case 4: note.Frets.Black2 = DualTime.Truncate(length); break;
                case 5:
                    if (note.State == GuitarState.Natural)
                    {
                        note.State = GuitarState.Forced;
                    }
                    break;
                case 6: note.State = GuitarState.Tap; break;
                case 7: note.Frets.Open =   DualTime.Truncate(length); break;
                case 8: note.Frets.Black3 = DualTime.Truncate(length); break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(ref DrumNote2<FourLane> note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0:  note.Bass = DualTime.Truncate(length); break;
                case 1:  note.Pads.Snare.Duration  = DualTime.Truncate(length); break;
                case 2:  note.Pads.Yellow.Duration = DualTime.Truncate(length); break;
                case 3:  note.Pads.Blue.Duration   = DualTime.Truncate(length); break;
                case 4:  note.Pads.Green.Duration  = DualTime.Truncate(length); break;

                case 32: note.ToggleDoubleBass(); break;

                case 34: note.Pads.Snare.Dynamics  = DrumDynamics.Accent; break;
                case 35: note.Pads.Yellow.Dynamics = DrumDynamics.Accent; break;
                case 36: note.Pads.Blue.Dynamics   = DrumDynamics.Accent; break;
                case 37: note.Pads.Green.Dynamics  = DrumDynamics.Accent; break;

                case 40: note.Pads.Snare.Dynamics  = DrumDynamics.Ghost; break;
                case 41: note.Pads.Yellow.Dynamics = DrumDynamics.Ghost; break;
                case 42: note.Pads.Blue.Dynamics   = DrumDynamics.Ghost; break;
                case 43: note.Pads.Green.Dynamics  = DrumDynamics.Ghost; break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(ref DrumNote2<FiveLane> note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0:  note.Bass = DualTime.Truncate(length); break;
                case 1:  note.Pads.Snare.Duration  = DualTime.Truncate(length); break;
                case 2:  note.Pads.Yellow.Duration = DualTime.Truncate(length); break;
                case 3:  note.Pads.Blue.Duration   = DualTime.Truncate(length); break;
                case 4:  note.Pads.Orange.Duration = DualTime.Truncate(length); break;
                case 5:  note.Pads.Green.Duration  = DualTime.Truncate(length); break;

                case 32: note.ToggleDoubleBass(); break;

                case 34: note.Pads.Snare.Dynamics  = DrumDynamics.Accent; break;
                case 35: note.Pads.Yellow.Dynamics = DrumDynamics.Accent; break;
                case 36: note.Pads.Blue.Dynamics   = DrumDynamics.Accent; break;
                case 37: note.Pads.Orange.Dynamics = DrumDynamics.Accent; break;
                case 38: note.Pads.Green.Dynamics  = DrumDynamics.Accent; break;

                case 40: note.Pads.Snare.Dynamics  = DrumDynamics.Ghost; break;
                case 41: note.Pads.Yellow.Dynamics = DrumDynamics.Ghost; break;
                case 42: note.Pads.Blue.Dynamics   = DrumDynamics.Ghost; break;
                case 43: note.Pads.Orange.Dynamics = DrumDynamics.Ghost; break;
                case 44: note.Pads.Green.Dynamics  = DrumDynamics.Ghost; break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(ref ProDrumNote2<FourLane> note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0:  note.Bass = DualTime.Truncate(length); break;
                case 1:  note.Pads.Snare.Duration  = DualTime.Truncate(length); break;
                case 2:  note.Pads.Yellow.Duration = DualTime.Truncate(length); break;
                case 3:  note.Pads.Blue.Duration   = DualTime.Truncate(length); break;
                case 4:  note.Pads.Green.Duration  = DualTime.Truncate(length); break;

                case 32: note.ToggleDoubleBass(); break;

                case 34: note.Pads.Snare.Dynamics  = DrumDynamics.Accent; break;
                case 35: note.Pads.Yellow.Dynamics = DrumDynamics.Accent; break;
                case 36: note.Pads.Blue.Dynamics   = DrumDynamics.Accent; break;
                case 37: note.Pads.Green.Dynamics  = DrumDynamics.Accent; break;

                case 40: note.Pads.Snare.Dynamics  = DrumDynamics.Ghost; break;
                case 41: note.Pads.Yellow.Dynamics = DrumDynamics.Ghost; break;
                case 42: note.Pads.Blue.Dynamics   = DrumDynamics.Ghost; break;
                case 43: note.Pads.Green.Dynamics  = DrumDynamics.Ghost; break;

                case 66: note.Cymbals[0] = true; break;
                case 67: note.Cymbals[1] = true; break;
                case 68: note.Cymbals[2] = true; break;
                default:
                    return false;
            }
            return true;
        }

        private static DrumsType _unknownDrumType;
        private static bool Set(ref ProDrumNote2<FiveLane> note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note.Bass = DualTime.Truncate(length); break;
                case 1: note.Pads.Snare.Duration  = DualTime.Truncate(length); break;
                case 2: note.Pads.Yellow.Duration = DualTime.Truncate(length); break;
                case 3: note.Pads.Blue.Duration   = DualTime.Truncate(length); break;
                case 4: note.Pads.Orange.Duration = DualTime.Truncate(length); break;
                case 5:
                    if ((_unknownDrumType & DrumsType.FiveLane) != DrumsType.FiveLane)
                    {
                        return false;
                    }
                    note.Pads.Green.Duration = DualTime.Truncate(length);
                    _unknownDrumType = DrumsType.FiveLane;
                    break;
                case 32: note.ToggleDoubleBass(); break;

                case 34: note.Pads.Snare.Dynamics  = DrumDynamics.Accent; break;
                case 35: note.Pads.Yellow.Dynamics = DrumDynamics.Accent; break;
                case 36: note.Pads.Blue.Dynamics   = DrumDynamics.Accent; break;
                case 37: note.Pads.Orange.Dynamics = DrumDynamics.Accent; break;
                case 38: note.Pads.Green.Dynamics  = DrumDynamics.Accent; break;

                case 40: note.Pads.Snare.Dynamics  = DrumDynamics.Ghost; break;
                case 41: note.Pads.Yellow.Dynamics = DrumDynamics.Ghost; break;
                case 42: note.Pads.Blue.Dynamics   = DrumDynamics.Ghost; break;
                case 43: note.Pads.Orange.Dynamics = DrumDynamics.Ghost; break;
                case 44: note.Pads.Green.Dynamics  = DrumDynamics.Ghost; break;

                case 66:
                case 67:
                case 68:
                    if ((_unknownDrumType & DrumsType.ProDrums) == DrumsType.ProDrums)
                    {
                        note.Cymbals[lane - 66] = true;
                        _unknownDrumType = DrumsType.ProDrums;
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
