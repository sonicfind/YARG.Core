using System;
using System.Linq;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.Logging;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public enum OverdriveStyle
    {
        GH,
        RB
    }

    public struct OverdriveInfo
    {
        public long   Amount;
        public bool   Completed;
        public double TimeOfCompletion;
    }

    public class OverdriveTracker
    {
        /// <summary>
        /// The minimum common factor that divides evenly with all the possible overdrive divisors
        /// </summary>
        /// <remarks>
        /// For gains (beat-based), 480 evenly divides by 30 (16).<br></br>
        /// For GH style drains (measure-based), 480 evenly divides by 8 (60).<br></br>
        /// For RB style drains (beat-based), 480 evenly divides by 32 (15).<br></br>
        /// </remarks>
        private const long MININUM_COMMON_MULTPLE = 480;
        public const long MAX_OVERDRIVE        = int.MaxValue / MININUM_COMMON_MULTPLE * MININUM_COMMON_MULTPLE;
        public const long ACTIVATION_THRESHOLD = MAX_OVERDRIVE / 2;
        public const long GAIN_PER_BEAT        = MAX_OVERDRIVE / 30;
        public const long DRAIN_PER_BEAT       = -MAX_OVERDRIVE / 32;
        public const long DRAIN_PER_MEASURE    = -MAX_OVERDRIVE / 8;

        private readonly YARGChart                                   _chart;
        private readonly OverdriveStyle                              _style;
        private readonly YargNativeList<(double EndTime, long Rate)> _gains = new();
        private readonly YargNativeList<(double Time, long Amount)>  _route = new();
        private          long                                        _baseOverdrive;
        private          bool                                        _isActive;
        private          double                                      _timeTracker;

        public bool IsActive => _isActive;

        public OverdriveTracker(YARGChart chart, OverdriveStyle style)
        {
            _chart = chart;
            _style = style;
        }

        public void Reset(in DualTime time)
        {
            _gains.Clear();
            _route.Clear();
            _isActive = false;
            _baseOverdrive = 0;
            _timeTracker = time.Seconds;
        }

        public OverdriveInfo GetOverdrive(in DualTime time)
        {
            YargLogger.Assert(_timeTracker <= time.Seconds,
                "Backwards time progression encountered! A reset is needed before this operation!");
            _timeTracker = time.Seconds;

            var info = default(OverdriveInfo);
            while (_route.Count >= 2 && _route[1].Time <= time.Seconds)
            {
                _route.RemoveAt(0);
                _baseOverdrive = _route[0].Amount;
                if (_route[0].Amount > 0)
                {
                    continue;
                }

                if (_isActive)
                {
                    info.Completed = true;
                    info.TimeOfCompletion = _route[0].Time;
                }
                _isActive = false;
            }

            info.Amount = _baseOverdrive;
            if (_route.Count >= 2)
            {
                double timeDiff = _route[1].Time - _route[0].Time;
                long overdriveDiff = _route[1].Amount - _route[0].Amount;

                double timeDisplacement = time.Seconds - _route[0].Time;
                info.Amount += (long)Math.Round(overdriveDiff * timeDisplacement / timeDiff);
            }
            return info;
        }

        public void AddOverdrive(in DualTime time, long overdrive)
        {
            _baseOverdrive = GetOverdrive(time).Amount + overdrive;
            if (_baseOverdrive > MAX_OVERDRIVE)
            {
                _baseOverdrive = MAX_OVERDRIVE;
            }

            if (!_route.IsEmpty())
            {
                if (!ResetRoute(in time, out long startBeat))
                {
                    return;
                }

                while (_gains.Count > 0 && _gains[0].EndTime <= time.Seconds)
                {
                    _gains.RemoveAt(0);
                }
                BuildRoute(in time, startBeat);
            }
        }

        public void StartGains(in DualTime time, in DualTime endTime)
        {
            _baseOverdrive = GetOverdrive(time).Amount;
            _gains.Clear();

            if (!ResetRoute(in time, out long startBeat))
            {
                return;
            }

            if (_style == OverdriveStyle.RB)
            {
                long nextBeat = startBeat + 1;
                while (_chart.BeatMap[startBeat].Value == BeatlineType.Weak && startBeat > 0)
                {
                    --startBeat;
                }

                while (_chart.BeatMap[nextBeat].Value == BeatlineType.Weak && nextBeat < _chart.BeatMap.Count - 1)
                {
                    ++nextBeat;
                }

                double beatDisplacement = _chart.BeatMap[nextBeat].Key.Seconds - _chart.BeatMap[startBeat].Key.Seconds;
                _gains.Add(
                    (
                        _chart.BeatMap[nextBeat].Key.Seconds,
                        (long) Math.Round(GAIN_PER_BEAT / beatDisplacement)
                    )
                );

                if (time.Seconds > _chart.BeatMap[startBeat].Key.Seconds && nextBeat < _chart.BeatMap.Count - 1)
                {
                    var percentage = (time.Seconds - _chart.BeatMap[startBeat].Key.Seconds) / beatDisplacement;

                    long nextNextBeat = nextBeat + 1;
                    while (_chart.BeatMap[nextNextBeat].Value == BeatlineType.Weak && nextNextBeat < _chart.BeatMap.Count - 1)
                    {
                        ++nextNextBeat;
                    }

                    beatDisplacement = _chart.BeatMap[nextNextBeat].Key.Seconds - _chart.BeatMap[nextBeat].Key.Seconds;
                    _gains.Add(
                        (
                            beatDisplacement * percentage,
                            (long) Math.Round(GAIN_PER_BEAT / beatDisplacement)
                        )
                    );
                }
            }
            else
            {
                long endTick = time.Ticks + _chart.Resolution;
                long currentIndex = startBeat;
                while (currentIndex + 1 < _chart.BeatMap.Count && _chart.BeatMap[currentIndex + 1].Key.Ticks <= endTick)
                {
                    ++currentIndex;
                }

                ref readonly var currBeat = ref _chart.BeatMap[currentIndex];
                double endSeconds = currBeat.Key.Seconds;
                if (currentIndex + 1 < _chart.BeatMap.Count)
                {
                    var beatDiff = _chart.BeatMap[currentIndex + 1].Key - currBeat.Key;
                    long tickDisplacement = endTick - currBeat.Key.Ticks;
                    endSeconds += beatDiff.Seconds * tickDisplacement / beatDiff.Ticks;
                }

                _gains.Add(
                    (
                        endSeconds,
                        (long) Math.Round(GAIN_PER_BEAT / (endSeconds - time.Seconds))
                    )
                );
            }

            BuildRoute(in time, startBeat);
        }

        public void StopGains(in DualTime time)
        {
            _baseOverdrive = GetOverdrive(time).Amount;
            _gains.Clear();

            if (_isActive && ResetRoute(in time, out long startBeat))
            {
                BuildRoute(in time, startBeat);
            }
        }

        public bool ActivateOverdrive(in DualTime time)
        {
            long overdrive = GetOverdrive(in time).Amount;
            if (_isActive ||
                overdrive < ACTIVATION_THRESHOLD ||
                !ResetRoute(in time, out long startBeat))
            {
                return false;
            }

            _baseOverdrive = overdrive;
            _isActive = true;
            while (_gains.Count > 0 && _gains[0].EndTime <= time.Seconds)
            {
                _gains.RemoveAt(0);
            }
            BuildRoute(in time, startBeat);
            return true;
        }

        private void BuildRoute(in DualTime time, long startBeat)
        {
            long gainIndex = 0;
            _route.Add((time.Seconds, _baseOverdrive));
            if (_isActive)
            {
                if (_style == OverdriveStyle.RB)
                {
                    while (_chart.BeatMap[startBeat].Value == BeatlineType.Weak && startBeat > 0)
                    {
                        --startBeat;
                    }
                }
                else
                {
                    while (_chart.BeatMap[startBeat].Value != BeatlineType.Measure && startBeat > 0)
                    {
                        --startBeat;
                    }
                }

                long nextBeat = startBeat;
                bool updateDrain = true;
                double drainPerSecond = 0;
                while (true)
                {
                    ref readonly var prevNode = ref _route[_route.Count - 1];
                    if (prevNode.Amount == 0)
                    {
                        break;
                    }

                    if (updateDrain)
                    {
                        nextBeat = startBeat + 1;
                        if (_style == OverdriveStyle.RB)
                        {
                            while (_chart.BeatMap[nextBeat].Value == BeatlineType.Weak && nextBeat < _chart.BeatMap.Count - 1)
                            {
                                ++nextBeat;
                            }
                            drainPerSecond = DRAIN_PER_BEAT / (_chart.BeatMap[nextBeat].Key.Seconds - _chart.BeatMap[startBeat].Key.Seconds);
                        }
                        else
                        {
                            while (_chart.BeatMap[nextBeat].Value != BeatlineType.Measure && nextBeat < _chart.BeatMap.Count - 1)
                            {
                                ++nextBeat;
                            }
                            drainPerSecond = DRAIN_PER_MEASURE / (_chart.BeatMap[nextBeat].Key.Seconds - _chart.BeatMap[startBeat].Key.Seconds);
                        }
                        startBeat = nextBeat;
                    }

                    double changePerSecond = drainPerSecond;
                    double endTime = _chart.BeatMap[nextBeat].Key.Seconds;

                    updateDrain = true;
                    bool updateGain = false;
                    if (gainIndex < _gains.Count)
                    {
                        changePerSecond += _gains[gainIndex].Rate;
                        if (_gains[gainIndex].EndTime < _chart.BeatMap[nextBeat].Key.Seconds)
                        {
                            endTime = _gains[gainIndex].EndTime;
                            updateDrain = false;
                        }
                        updateGain = _gains[gainIndex].EndTime <= _chart.BeatMap[nextBeat].Key.Seconds;
                    }

                    double timeDiff = endTime - prevNode.Time;
                    long overdriveChange = (long) Math.Round(changePerSecond * timeDiff);
                    long nextAmount = prevNode.Amount + overdriveChange;
                    if (nextAmount < 0)
                    {
                        // Negative amount because the change is negative
                        double timeToZero = -prevNode.Amount / changePerSecond;
                        _route.Add((prevNode.Time + timeToZero, 0));
                    }
                    else
                    {
                        if (nextAmount > MAX_OVERDRIVE)
                        {
                            nextAmount = MAX_OVERDRIVE;
                            if (prevNode.Amount < MAX_OVERDRIVE)
                            {
                                long changeToOne = MAX_OVERDRIVE - prevNode.Amount;
                                double timeToOne = changeToOne / changePerSecond;
                                _route.Add((prevNode.Time + timeToOne, MAX_OVERDRIVE));
                            }
                        }
                        _route.Add((endTime, nextAmount));

                        if (updateGain)
                        {
                            gainIndex++;
                        }
                    }
                }
            }

            while (gainIndex < _gains.Count)
            {
                ref readonly var gain = ref _gains[gainIndex];
                ref readonly var prevNode = ref _route[_route.Count - 1];
                double timeDiff = gain.EndTime - prevNode.Time;
                long overdriveChange = (long) Math.Round(gain.Rate * timeDiff);
                long nextAmount = prevNode.Amount + overdriveChange;
                if (nextAmount > MAX_OVERDRIVE)
                {
                    nextAmount = MAX_OVERDRIVE;
                    if (prevNode.Amount < MAX_OVERDRIVE)
                    {
                        long changeToOne = MAX_OVERDRIVE - prevNode.Amount;
                        double timeToOne = changeToOne / gain.Rate;
                        _route.Add((prevNode.Time + timeToOne, MAX_OVERDRIVE));
                    }
                }
                _route.Add((gain.EndTime, nextAmount));
                ++gainIndex;
            }
        }

        private bool ResetRoute(in DualTime time, out long startBeat)
        {
            _route.Clear();
            startBeat = _chart.BeatMap.Find(time);
            if (startBeat < 0)
            {
                startBeat = ~startBeat - 1;
            }
            return startBeat < _chart.BeatMap.Count - 1;
        }
    }
}