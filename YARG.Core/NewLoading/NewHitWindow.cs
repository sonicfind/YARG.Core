using System;

namespace YARG.Core.NewLoading
{
    public class NewHitWindow
    {
        public double FrontMax { get; set; }
        public double FrontMin { get; set; }
        public double BackMax  { get; set; }
        public double BackMin  { get; set; }

        /// <summary>
        /// Toggles whether the hit window can shrink depending on the distance between two notes
        /// </summary>
        public bool IsDynamic { get; set; }

        public double DynamicWindowSlope;
        public double DynamicWindowScale;
        public double DynamicWindowGamma;

        public double CalculateFrontEnd(double noteDistance)
        {
            double front = FrontMax;
            if (IsDynamic && noteDistance < FrontMax + BackMax)
            {
                front *= noteDistance / (FrontMax + BackMax);
            }
            return front > FrontMin ? front : FrontMin;
        }

        public double CalculateBackEnd(double noteDistance)
        {
            double back = FrontMax;
            if (IsDynamic && noteDistance < FrontMax + BackMax)
            {
                back *= noteDistance / (FrontMax + BackMax);
            }
            return back > BackMin ? back : BackMin;
        }

        private double Dark_Yarg_Impl(double noteDistance, double max, double min)
        {
            double maxMultiScale = max * DynamicWindowScale;
            double minMultiSlope = min * DynamicWindowSlope;
            double gammaPow = Math.Pow(noteDistance / maxMultiScale, DynamicWindowGamma);
            double realSize = gammaPow * (max - minMultiSlope) + minMultiSlope;
            return Math.Clamp(realSize, min, max);
        }
    }
}
