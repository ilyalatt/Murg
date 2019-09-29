using System;

namespace Murg.Cli
{
    sealed class CliEnvironment
    {
        public readonly CliArguments Args;
        public readonly Action<string> WriteInfoLine;
        public readonly Action<double> UpdateProgress;

        public CliEnvironment(CliArguments args, Action<string> writeInfoLine, Action<double> updateProgress)
        {
            Args = args;
            WriteInfoLine = writeInfoLine;
            UpdateProgress = updateProgress;
        }
    }
}