using System.IO;

namespace YARG.Core.Engine.Drums
{
    public class DrumsStats : BaseStats
    {

        /// <summary>
        /// Number of overhits which have occurred.
        /// </summary>
        public int Overhits;

        public DrumsStats()
        {
        }

        public DrumsStats(DrumsStats stats) : base(stats)
        {

        }

        public DrumsStats(BinaryReader reader)
            : base(reader) { }
    }
}