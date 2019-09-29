using System;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Murg.Cli
{
    static class CliArgumentsParser
    {
        static Option<CliArguments> Parse(string[] args)
        {
            var parser = new Parser(settings => settings.AutoHelp = false);
            var parseResult = parser.ParseArguments<CliArguments>(args);
            Option<CliArguments> result = None;
            parseResult.WithParsed(cliArgs => result = cliArgs);
            parseResult.WithNotParsed(errs =>
            {
                var helpText = HelpText.AutoBuild(parseResult);
                helpText.Copyright = "";
                Console.Error.WriteLine(helpText);
            });
            return result;
        }

        static Option<CliArguments> Validate(CliArguments args)
        {
            var badDirs = args.Directories.Filter(dir => !Directory.Exists(dir)).ToList();

            if (badDirs.Count > 0)
            {
                Console.Error.WriteLine("Can not find following directories:");
                badDirs.ForEach(Console.Error.WriteLine);
                return None;
            }

            return args;
        }

        public static Option<CliArguments> ParseAndValidate(string[] args) =>
            args.Apply(Parse).Bind(Validate);
    }
}