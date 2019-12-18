using System;
using System.Collections.Generic;

namespace SmartCommandLineParser
{
    internal class Option
    {
        public string Name;
        public object DefaultValue;
        public Type Type;
        public IEnumerable<string> Switches;
        public string Help;
        public bool Repeatable;
        public bool Required;
    }
}