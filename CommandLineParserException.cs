using System;

namespace SmartCommandLineParser
{
    public class CommandLineParserException : Exception
    {
        public CommandLineParserException(string message) : base(message) {}
    }
}