using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum DrumDynamics
    {
        None,
        Accent,
        Ghost
    }

    public interface IDrumPad
    {
        public DualTime Duration { get; set; }
        public DrumDynamics Dynamics { get; set; }
        public bool IsActive();
    }

    public struct DrumPad : IDrumPad
    {
        private DualTime _duration;
        private DrumDynamics _dynamics;

        public DualTime Duration
        {
            readonly get => _duration;
            set => _duration = value;
        }

        public DrumDynamics Dynamics
        {
            readonly get => _dynamics;
            set => _dynamics = value;
        }

        public readonly bool IsActive()
        {
            return _duration.IsActive();
        }

        public readonly override string ToString()
        {
            return _dynamics != DrumDynamics.None ? $"{_duration.Ticks} - {_dynamics}" : _duration.Ticks.ToString();
        }
    }

    public struct DrumPad_Pro : IDrumPad
    {
        private DualTime _duration;
        private DrumDynamics _dynamics;
        public bool CymbalFlag;

        public DualTime Duration
        {
            readonly get => _duration;
            set => _duration = value;
        }

        public DrumDynamics Dynamics
        {
            readonly get => _dynamics;
            set => _dynamics = value;
        }

        public readonly bool IsActive()
        {
            return _duration.IsActive();
        }

        public readonly override string ToString()
        {
            string str = _dynamics != DrumDynamics.None ? $"{_duration.Ticks} - {_dynamics}" : _duration.Ticks.ToString();
            if (CymbalFlag)
            {
                str += " - Cymbal";
            }
            return str;
        }
    }
}
