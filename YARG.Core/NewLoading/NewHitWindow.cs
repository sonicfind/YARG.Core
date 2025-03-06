using System;

namespace YARG.Core.NewLoading
{
    public struct NewHitWindow
    {
        public double FrontMax;
        public double FrontMin;
        public double BackMax;
        public double BackMin;

        /// <summary>
        /// Whether the hit window size can change over time.
        /// This is usually done by looking at the time in between notes.
        /// </summary>
        public bool IsDynamic;

        public double DynamicWindowSlope;
        public double DynamicWindowScale;
        public double DynamicWindowGamma;

        public readonly double CalculateFrontEnd(double noteDistance)
        {
            double front = FrontMax;
            if (IsDynamic && noteDistance < FrontMax + BackMax)
            {
                front *= noteDistance / (FrontMax + BackMax);
            }
            return front > FrontMin ? front : FrontMin;
        }

        public readonly double CalculateBackEnd(double noteDistance)
        {
            double back = FrontMax;
            if (IsDynamic && noteDistance < FrontMax + BackMax)
            {
                back *= noteDistance / (FrontMax + BackMax);
            }
            return back > BackMin ? back : BackMin;
        }

        private readonly double Dark_Yarg_Impl(double noteDistance, double max, double min)
        {
            double maxMultiScale = max * DynamicWindowScale;
            double minMultiSlope = min * DynamicWindowSlope;
            double gammaPow = Math.Pow(noteDistance / maxMultiScale, DynamicWindowGamma);
            double realSize = gammaPow * (max - minMultiSlope) + minMultiSlope;
            return Math.Clamp(realSize, min, max);
        }
    }
}