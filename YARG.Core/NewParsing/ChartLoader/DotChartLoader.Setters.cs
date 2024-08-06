using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public unsafe partial class YARGChart
    {
        private static bool Set(FiveFretGuitar* note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note->Green  = DualTime.Truncate(length); break;
                case 1: note->Red    = DualTime.Truncate(length); break;
                case 2: note->Yellow = DualTime.Truncate(length); break;
                case 3: note->Blue   = DualTime.Truncate(length); break;
                case 4: note->Orange = DualTime.Truncate(length); break;
                case 5:
                    if (note->State == GuitarState.Natural)
                    {
                        note->State = GuitarState.Forced;
                    }
                    break;
                case 6: note->State = GuitarState.Tap; break;
                case 7: note->Open = DualTime.Truncate(length); break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(SixFretGuitar* note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note->White1 = DualTime.Truncate(length); break;
                case 1: note->White2 = DualTime.Truncate(length); break;
                case 2: note->White3 = DualTime.Truncate(length); break;
                case 3: note->Black1 = DualTime.Truncate(length); break;
                case 4: note->Black2 = DualTime.Truncate(length); break;
                case 5:
                    if (note->State == GuitarState.Natural)
                    {
                        note->State = GuitarState.Forced;
                    }
                    break;
                case 6: note->State = GuitarState.Tap; break;
                case 7: note->Open   = DualTime.Truncate(length); break;
                case 8: note->Black3 = DualTime.Truncate(length); break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(FourLaneDrums* note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note->Bass   = DualTime.Truncate(length); break;
                case 1: note->Snare  = DualTime.Truncate(length); break;
                case 2: note->Yellow = DualTime.Truncate(length); break;
                case 3: note->Blue   = DualTime.Truncate(length); break;
                case 4: note->Green  = DualTime.Truncate(length); break;

                case 32: note->IsDoubleBass = true; break;

                case 34: note->Dynamics_Snare  = DrumDynamics.Accent; break;
                case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                case 36: note->Dynamics_Blue   = DrumDynamics.Accent; break;
                case 37: note->Dynamics_Green  = DrumDynamics.Accent; break;

                case 40: note->Dynamics_Snare  = DrumDynamics.Ghost; break;
                case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                case 42: note->Dynamics_Blue   = DrumDynamics.Ghost; break;
                case 43: note->Dynamics_Green  = DrumDynamics.Ghost; break;

                case 66: note->Cymbal_Yellow = true; break;
                case 67: note->Cymbal_Blue = true; break;
                case 68: note->Cymbal_Green = true; break;
                default:
                    return false;
            }
            return true;
        }

        private static bool Set(FiveLaneDrums* note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0:  note->Bass   = DualTime.Truncate(length); break;
                case 1:  note->Snare  = DualTime.Truncate(length); break;
                case 2:  note->Yellow = DualTime.Truncate(length); break;
                case 3:  note->Blue   = DualTime.Truncate(length); break;
                case 4:  note->Orange = DualTime.Truncate(length); break;
                case 5:  note->Green  = DualTime.Truncate(length); break;

                case 32: note->IsDoubleBass = true; break;

                case 34: note->Dynamics_Snare  = DrumDynamics.Accent; break;
                case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                case 36: note->Dynamics_Blue   = DrumDynamics.Accent; break;
                case 37: note->Dynamics_Orange = DrumDynamics.Accent; break;
                case 38: note->Dynamics_Green  = DrumDynamics.Accent; break;

                case 40: note->Dynamics_Snare  = DrumDynamics.Ghost; break;
                case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                case 42: note->Dynamics_Blue   = DrumDynamics.Ghost; break;
                case 43: note->Dynamics_Orange = DrumDynamics.Ghost; break;
                case 44: note->Dynamics_Green  = DrumDynamics.Ghost; break;
                default:
                    return false;
            }
            return true;
        }

        private static DrumsType _unknownDrumType;
        private static bool Set(UnknownLaneDrums* note, int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: note->Bass   = DualTime.Truncate(length); break;
                case 1: note->Snare  = DualTime.Truncate(length); break;
                case 2: note->Yellow = DualTime.Truncate(length); break;
                case 3: note->Blue   = DualTime.Truncate(length); break;
                case 4: note->Orange = DualTime.Truncate(length); break;
                case 5:
                    if (_unknownDrumType != DrumsType.Unknown && _unknownDrumType != DrumsType.FiveLane)
                    {
                        return false;
                    }
                    note->Green = DualTime.Truncate(length);
                    _unknownDrumType = DrumsType.FiveLane;
                    break;
                case 32: note->IsDoubleBass = true; break;

                case 34: note->Dynamics_Snare  = DrumDynamics.Accent; break;
                case 35: note->Dynamics_Yellow = DrumDynamics.Accent; break;
                case 36: note->Dynamics_Blue   = DrumDynamics.Accent; break;
                case 37: note->Dynamics_Orange = DrumDynamics.Accent; break;
                case 38: note->Dynamics_Green  = DrumDynamics.Accent; break;

                case 40: note->Dynamics_Snare  = DrumDynamics.Ghost; break;
                case 41: note->Dynamics_Yellow = DrumDynamics.Ghost; break;
                case 42: note->Dynamics_Blue   = DrumDynamics.Ghost; break;
                case 43: note->Dynamics_Orange = DrumDynamics.Ghost; break;
                case 44: note->Dynamics_Green  = DrumDynamics.Ghost; break;

                case 66:
                    if (_unknownDrumType != DrumsType.FiveLane) unsafe
                    {
                        note->Cymbal_Yellow = true;
                        _unknownDrumType = DrumsType.ProDrums;
                    }
                    break;
                case 67:
                    if (_unknownDrumType != DrumsType.FiveLane) unsafe
                    {
                        note->Cymbal_Blue = true;
                        _unknownDrumType = DrumsType.ProDrums;
                    }
                    break;
                case 68:
                    if (_unknownDrumType != DrumsType.FiveLane) unsafe
                    {
                        note->Cymbal_Orange = true;
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
