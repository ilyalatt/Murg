using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discogs.ApiClient;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Murg.Backend;
using Murg.Backend.InfoExtraction;
using Murg.Backend.Input;
using Murg.Cli;
using ShellProgressBar;

namespace Murg
{
    static class Program
    {
        static async Task ProcessDir(CliEnvironment env, DiscogsApiClient discogsApiClient, TrackDirInfo trackDirInfo)
        {
            var dirPath = trackDirInfo.Path;
                    
            var outputRootOpt = await Matcher.Match(discogsApiClient, trackDirInfo);
            if (outputRootOpt.IsNone) return;
            var outputRoot = outputRootOpt.ValueUnsafe();
            
            foreach (var track in outputRoot.Tracks)
            {
                var trackNewFileName = $"{track.TrackNumber:00}. {track.Title}{Path.GetExtension(track.Path)}";

                var trackOldPath = track.Path;
                var trackNewPath = Path.Combine(dirPath, trackNewFileName);
                if (trackOldPath != trackNewPath)
                {
                    env.WriteInfoLine($"Rename '{Path.GetFileName(track.Path)}' -> '{trackNewFileName}'");
                    if (!env.Args.Dry)
                    {
                        File.Move(trackOldPath, trackNewPath);
                    }
                }

                if (!env.Args.Dry)
                {
                    var tagLibFile = TagLib.File.Create(trackNewPath);
                    var tags = tagLibFile.Tag;
                    tags.Performers = outputRoot.Performers.ToArray();
                    tags.Album = outputRoot.Album;
                    tags.Title = track.Title;
                    tags.Track = (uint) track.TrackNumber;
                    tagLibFile.Save();
                }
            }

            var newDirPath = Path.Combine(Path.GetDirectoryName(dirPath), outputRoot.Album);

            if (dirPath == newDirPath) return;
            env.WriteInfoLine($"Rename '{dirPath}' -> '{newDirPath}'");
            if (!env.Args.Dry)
            {
                Directory.Move(dirPath, newDirPath);
            }
        }

        static async Task<List<TrackDirInfo>> GetWorkingDirectories(CliArguments args)
        {
            var baseDirs = args.Directories
                .Map(Path.GetFullPath)
                .Map(x => x.TrimEnd('/'))
                .Distinct()
                .ToList();

            IEnumerable<string> SubDirs(string dir) =>
                Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);

            var dirs = !args.Recursive ? baseDirs : baseDirs.Concat(baseDirs.Bind(SubDirs));
            var inputRoots = await dirs.Map(x => FsReader.GetInputRoot(x)).Apply(Task.WhenAll);
            return inputRoots.Map(TrackDirInfoExtractor.Extract).ToList();
        }

        static async Task Work(CliEnvironment env)
        {
            var args = env.Args;
            var client = DiscogsApiClientProvider.CreateDiscogsApiClient();

            var workingDirs = await GetWorkingDirectories(args);
            
            foreach (var dir in workingDirs.Filter(x => x.Tracks.Count < 2))
            {
                env.WriteInfoLine($"Ignoring {dir.Path}");
            }
            workingDirs = workingDirs.Filter(x => x.Tracks.Count >= 2).ToList();
            
            var counter = 0;
            void Up() => env.UpdateProgress((double) counter++ / workingDirs.Count);

            foreach (var dir in workingDirs)
            {
                env.WriteInfoLine($"Processing {dir.Path}");
                await ProcessDir(env, client, dir);
                Up();
            }
        }

        static CliEnvironment CreateCliEnvironment(CliArguments args)
        {
#if DEBUG
            const char ProgressCharacter = '#'; // Rider console does not like long dash
#else
            const char ProgressCharacter = '─';
#endif
            var options = new ProgressBarOptions
            {
                ProgressCharacter = ProgressCharacter,
                ProgressBarOnBottom = true
            };

            var progressBar = new ProgressBar(100, "", options);

            return new CliEnvironment(
                args: args,
                writeInfoLine: progressBar.WriteLine,
                updateProgress: p => progressBar.Tick((int) (p * 100))
            );
        }

        static async Task<int> Main(string[] args)
        {
            var cliArgsOpt = CliArgumentsParser.ParseAndValidate(args);
            if (cliArgsOpt.IsNone) return 1;

            var cliArgs = cliArgsOpt.ValueUnsafe();
            var cliEnv = CreateCliEnvironment(cliArgs);

            await Work(cliEnv);
            return 0;
        }
    }
}
