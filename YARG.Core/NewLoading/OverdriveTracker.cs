using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public enum OverdriveStyle
    {
        GH,
        RB
    }

    public class OverdriveTracker
    {
        /// <summary>
        /// Representation of the max amount of overdrive
        /// </summary>
        /// <remarks>
        /// For gains (beat-based), 480 evenly divides by 30 (16).<br></br>
        /// For GH style drains (measure-based), 480 evenly divides by 8 (60).<br></br>
        /// For RB style drains (beat-based), 480 evenly divides by 32 (15).<br></br>
        /// </remarks>
        public const double MAX_OVERDRIVE = 480;
        public const double GAIN_PER_BEAT     = 16;
        public const double DRAIN_PER_BEAT    = 15;
        public const double DRAIN_PER_MEASURE = 60;

        private struct OverdriveNode
        {
            public long   Tick;
            public double Amount;
        }

        private readonly YargNativeList<OverdriveNode> _gains;
        private readonly YargNativeList<OverdriveNode> _nodes;
        private readonly OverdriveStyle                _style;
        private          long                          _startTick;
        private          double                        _overdriveBase;
        private          bool                          _isActive;

        public OverdriveTracker(OverdriveStyle style)
        {
            _gains = new YargNativeList<OverdriveNode>();
            _nodes = new YargNativeList<OverdriveNode>();
            _style = style;
            _startTick = 0;
            _overdriveBase = 0;
            _isActive = false;
        }

        public void StartGains(in DualTime time, YARGChart chart)
        {
            //_overdriveBase = GetOverdrive(time.Ticks);
            _startTick = time.Ticks;
            _nodes.Clear();

            long beatIndex = chart.BeatMap.Find(time);
            if (beatIndex < 0)
            {
                beatIndex = ~beatIndex;
            }

            if (beatIndex == chart.BeatMap.Count)
            {
                return;
            }

            if (_style == OverdriveStyle.RB)
            {

            }
        }


        // public double GetOverdrive(long tick)
        // {
        //     double overdrive = _overdriveBase;
        //     if (_drainIndex >= 0)
        //     {
        //         // Gain and Drain both start at the same tick initially
        //         long pivot = _startTick;
        //         while (_drainIndex + 1 < _beatMap.Count)
        //         {
        //             long nextBeat = _drainIndex + 1;
        //             if (_style == OverdriveStyle.GH)
        //             {
        //                 while (nextBeat + 1 < _beatMap.Count && _beatMap[nextBeat].Value != BeatlineType.Measure)
        //                 {
        //                     nextBeat++;
        //                 }
        //             }
        //             else
        //             {
        //                 while (nextBeat + 1 < _beatMap.Count && _beatMap[nextBeat].Value == BeatlineType.Weak)
        //                 {
        //                     nextBeat++;
        //                 }
        //             }
        //
        //             long drainTickDiff = _beatMap[nextBeat].Key.Ticks - _beatMap[_drainIndex].Key.Ticks;
        //             while (_gainIndex + 1 < _gains.Count)
        //             {
        //                 double gainAmountDiff = _gains[_gainIndex + 1].AmountOffset - _gains[_gainIndex].AmountOffset;
        //                 long gainTickDiff = _gains[_gainIndex + 1].Tick - _gains[_gainIndex].Tick;
        //
        //             }
        //
        //         }
        //         while (_drainIndex + 1 < _beatMap.Count && _gainIndex + 1 < _gains.Count)
        //         {
        //             double gainAmountDiff = _gains[_gainIndex + 1].AmountOffset - _gains[_gainIndex].AmountOffset;
        //             long gainTickDiff = _gains[_gainIndex + 1].Tick - _gains[_gainIndex].Tick;
        //
        //         }
        //         long i = _drainBeatIndex;
        //         while (i + 1 < _beatMap.Count && _beatMap[i + 1].Key.Ticks < tick)
        //         {
        //             if (_gainIndex < _gains.Count)
        //             {
        //
        //             }
        //         }
        //         for (long i = _drainBeatIndex; i < _beatMap.Count; i++)
        //         {
        //
        //         }
        //     }
        //     for (long i = 0; i < _gains.Count; i++)
        //     {
        //         if (i + 1 == _gains.Count)
        //         {
        //             overdrive += _gains[i].AmountOffset;
        //         }
        //         else if (tick < _gains[i + 1].Tick)
        //         {
        //             double amountDiff = _gains[i + 1].AmountOffset - _gains[i].AmountOffset;
        //             long tickDiff = _gains[i + 1].Tick - _gains[i].Tick;
        //             long factor = tick - _gains[i].Tick;
        //             overdrive += amountDiff * factor / tickDiff;
        //             break;
        //         }
        //     }
        //
        //     for (long i = 0; i < _drains.Count; i++)
        //     {
        //         if (i + 1 == _drains.Count)
        //         {
        //             overdrive += _gains[i].AmountOffset;
        //         }
        //         else if (tick < _gains[i + 1].Tick)
        //         {
        //             double amountDiff = _gains[i + 1].AmountOffset - _gains[i].AmountOffset;
        //             long tickDiff = _gains[i + 1].Tick - _gains[i].Tick;
        //             long factor = tick - _gains[i].Tick;
        //             double overdriveToAdd = (amountDiff * factor) / tickDiff;
        //             overdrive += overdriveToAdd;
        //             break;
        //         }
        //     }
        // }
    }
}