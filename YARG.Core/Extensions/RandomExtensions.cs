using System;

namespace YARG.Core.Extensions
{
    public static class RandomExtensions
    {
        public static void Shuffle<T>(this Random rand, Span<T> values)
        {
            int n = values.Length;
            for (int i = 0; i < n - 1; i++)
            {
                int j = rand.Next(i, n);
                if (j != i)
                {
                    (values[i], values[j]) = (values[j], values[i]);
                }
            }
        }
    }
}