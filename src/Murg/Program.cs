using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discogs.ApiClient;
using Discogs.ApiClient.ApiModel.Exceptions;
using Discogs.ApiClient.ApiModel.Responses.Database.GetRelease;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Murg.Backend;
using Murg.Backend.FsInfo;
using Murg.Cli;
using ShellProgressBar;
using static LanguageExt.Prelude;

namespace Murg
{
    static class Program
    {
        static bool CheckReleaseNameConsistency(string releaseName, string query)
        {
            // Numbers should be the same
            var getTokens = fun<string, IEnumerable<string>>(s => StringUtils.ExtractDigitGroups(s).OrderBy(x => x));
            var releaseTokens = getTokens(releaseName).ToList();
            var queryTokens = getTokens(query).ToList();
            return releaseTokens.SequenceEqual(queryTokens);
        }

        static IEnumerable<Release> CheckReleasesConsistency(TrackDirInfo dirInfo, IReadOnlyList<Release> releases)
        {
            if (releases.Count == 0) return releases;
            
            IEnumerable<string> GetTokens(string s) => StringUtils.SplitToTokens(s).Filter(x => x.Length > 3);
            
            var dirInfoTokens = dirInfo.Tracks.Map(x => x.MeaningfulTrackTitleTokens).Bind(GetTokens).ToList();
            var idealConformity = dirInfoTokens.Count;
            int MeasureTracklistConformity(IEnumerable<Track> tracklist)
            {
                var tokens = tracklist.Map(x => x.Title).Bind(GetTokens);
                return tokens.Intersect(dirInfoTokens).Count();
            }
            
            const int coefficient = 3;
            return releases
                .Map(release => (release, conformity: MeasureTracklistConformity(release.Tracklist)))
                .Filter(x => x.conformity >= idealConformity / coefficient)
                .Map(x => x.release);
        }
        
        static async Task<Arr<Release>> FindMatchingReleases(DiscogsApiClient discogsApiClient, TrackDirInfo dirInfo, string query, int tracksCount)
        {
            var db = discogsApiClient.Database;
            var searchResult = await db.Search(query);
            var goodBriefReleases = searchResult.Results
                .Filter(x => x.Id != x.MasterId)
                .Filter(x => CheckReleaseNameConsistency(x.Title, query))
                .Take(5);

            var releases = new List<Release>();
            foreach (var releaseBrief in goodBriefReleases)
            {
                try
                {
                    var release = await db.GetRelease(releaseBrief.Id);
                    if (release.Tracklist == null || release.Tracklist.Count != tracksCount) continue;
                    release.Tracklist = release.Tracklist.Filter(x => !string.IsNullOrEmpty(x.Position)).ToList(); // TODO

                    releases.Add(release);
                }
                catch (DiscogsApiException) { } // TODO: not found
            }

            return CheckReleasesConsistency(dirInfo, releases).ToArr();
        }

        static IEnumerable<string> DirNameToQueries(string dirName)
        {
            var allowedSymbols = StringUtils.GetCommonSymbols();
            var simplifiedName = new string(dirName.Where(x => allowedSymbols.Contains(x)).ToArray());
            
            var words = simplifiedName.Split(' ');
            return Range(0, words.Length - 1).Map(x => words.Take(words.Length - x))
                .Map(ws => string.Join(' ', ws));
        }

        static async Task ProcessDir(CliEnvironment env, DiscogsApiClient discogsApiClient, TrackDirInfo dirInfo)
        {
            var matchingReleases = Arr<Release>.Empty;
            var queries = dirInfo.CommonPrefix.Bind(DirNameToQueries).Concat(DirNameToQueries(Path.GetFileName(dirInfo.Path)));
            foreach (var q in queries)
            {
                if (!matchingReleases.IsEmpty) break;
                matchingReleases = await FindMatchingReleases(discogsApiClient, dirInfo, q, dirInfo.Tracks.Count);
            }

            if (matchingReleases.IsEmpty) return;

            var canonicity = fun<string, int>(StringUtils.MeasureStringCanonicity);

            var sortedReleases = matchingReleases
                .OrderByDescending(x => (x.Tracklist.Select(y => y.Title).Sum(canonicity), x.Community.Have)).ToArray();
            var bestRelease = sortedReleases.First();
            
            foreach (var trackFileInfo in dirInfo.Tracks)
            {
                if (matchingReleases.IsEmpty) return;

                // TODO: separate component and model for matching
                var releaseTrack = trackFileInfo.TrackNumber.Match(
                    n => sortedReleases.Map(x => x.Tracklist[n - 1]).OrderByDescending(x => canonicity(x.Title)).First(),
                    () => bestRelease.Tracklist
                        .OrderBy(x => StringUtils.CalculateLevenshteinDistance(x.Title, trackFileInfo.TrackTitle))
                        .First()
                );
                var isReleaseTrackMoreCanonical = canonicity(releaseTrack.Title) >= canonicity(trackFileInfo.TrackTitle);
                var isTrackTitleUseless = trackFileInfo.MeaningfulTrackTitleTokens.Length * 3 < releaseTrack.Title.Length;
                var trackTitle = isReleaseTrackMoreCanonical || isTrackTitleUseless
                    ? releaseTrack.Title.Trim()
                    : trackFileInfo.TrackTitle;
                var trackNewNumber = trackFileInfo.TrackNumber.IfNone(() => int.Parse(releaseTrack.Position));
                var trackNewFileName = $"{trackNewNumber:00}. {trackTitle}{Path.GetExtension(trackFileInfo.FileName)}";

                var trackOldPath = Path.Combine(dirInfo.Path, trackFileInfo.FileName);
                var trackNewPath = Path.Combine(dirInfo.Path, trackNewFileName);
                if (trackOldPath != trackNewPath)
                {
                    env.WriteInfoLine($"Rename '{trackFileInfo.FileName}' -> '{trackNewFileName}'");
                    if (!env.Args.Dry)
                    {
                        File.Move(trackOldPath, trackNewPath);
                    }
                }

                if (!env.Args.Dry)
                {
                    var tagLibFile = TagLib.File.Create(trackNewPath);
                    var tags = tagLibFile.Tag;
                    tags.Performers = bestRelease.Artists.Map(x => x.Name).ToArray();
                    tags.Album = bestRelease.Title;
                    tags.Title = trackTitle;
                    tags.Track = (uint) trackNewNumber;
                    tagLibFile.Save();
                }
            }

            var oldDirPath = dirInfo.Path;
            var bestReleaseTitle = sortedReleases.Map(x => x.Title)
                .Append(Path.GetFileName(dirInfo.Path))
                .OrderByDescending(canonicity)
                .First();
            var newDirPath = Path.Combine(Path.GetDirectoryName(oldDirPath), bestReleaseTitle);

            if (oldDirPath == newDirPath) return;
            env.WriteInfoLine($"Rename '{oldDirPath}' -> '{newDirPath}'");
            if (!env.Args.Dry)
            {
                Directory.Move(oldDirPath, newDirPath);
            }
        }

        static List<TrackDirInfo> GetWorkingDirectories(CliArguments args)
        {
            var baseDirs = args.Directories
                .Map(Path.GetFullPath)
                .Map(x => x.TrimEnd('/'))
                .Distinct()
                .ToList();

            IEnumerable<string> SubDirs(string dir) =>
                Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);

            var dirs = !args.Recursive ? baseDirs : baseDirs.Concat(baseDirs.Bind(SubDirs));
            return dirs.Map(TrackDirInfoExtractor.Extract).ToList();
        }

        static async Task Work(CliEnvironment env)
        {
            var args = env.Args;
            var client = DiscogsApiClientProvider.CreateDiscogsApiClient();

            var workingDirs = GetWorkingDirectories(args);
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
