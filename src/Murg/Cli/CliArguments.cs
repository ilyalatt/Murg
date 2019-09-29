using System.Collections.Generic;
using CommandLine;

namespace Murg.Cli
{
    sealed class CliArguments
    {
        [Value(0, Min = 1, MetaName = "DIRS", HelpText = "Directories to be processed.")]
        public IEnumerable<string> Directories { get; set; }

        [Option('r', "recursive", Default = false, HelpText = "Run the tool recursively.")]
        public bool Recursive { get; set; }

        [Option('d', "dry", Default = false, HelpText = "Run the tool but do not apply changes.")]
        public bool Dry { get; set; }
    }
}