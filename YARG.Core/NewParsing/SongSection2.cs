using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace YARG.Core.NewParsing
{
    public class SongSection2
    {
        private string _name;

        public string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException("name");
        }
        
        public SongSection2() { _name = string.Empty; }
        public SongSection2(string name)
        {
            _name = null!;
            Name = name;
        }

        public static implicit operator string(SongSection2 section) => section.Name;
        public static implicit operator SongSection2(string str) => new(str);
    }
}
