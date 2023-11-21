using System;
using System.Text;

namespace YARG.Core.Chart.Drums
{
    public interface ICymbalConfig { }

    public struct Basic_Drums : ICymbalConfig
    {
        public bool this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ToString()
        {
            return "Basic Drums";
        }
    }

    public struct Pro_Drums : ICymbalConfig
    {
        private unsafe fixed bool cymbals[3];
        public bool this[int index]
        {
            get
            {
                if (0 <= index && index < 3)
                {
                    unsafe
                    {
                        return cymbals[index];
                    }
                }
                throw new IndexOutOfRangeException();
            }

            set
            {
                if (0 <= index && index < 3)
                {
                    unsafe
                    {
                        cymbals[index] = value;
                    }
                }
                else
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            unsafe
            {
                if (cymbals[0])
                    builder.Append($"Y-Cymbal | ");
                if (cymbals[1])
                    builder.Append($"B-Cymbal | ");
                if (cymbals[2])
                    builder.Append($"G-Cymbal | ");
            }
            return builder.ToString();
        }
    }
}
