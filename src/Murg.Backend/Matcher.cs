using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discogs.ApiClient;
using Discogs.ApiClient.ApiModel.Exceptions;
using Discogs.ApiClient.ApiModel.Responses.Database.GetRelease;
using LanguageExt;
using Murg.Backend.InfoExtraction;
using Murg.Backend.Output;
using static LanguageExt.Prelude;

namespace Murg.Backend
{
    public static class Matcher
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

        static async Task<Arr<Release>> FindMatchingReleases(
            DiscogsApiClient discogsApiClient,
            TrackDirInfo dirInfo,
            string query,
            int tracksCount
        )
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
                    release.Tracklist =
                        release.Tracklist.Filter(x => !string.IsNullOrEmpty(x.Position)).ToList(); // TODO

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

        public static async Task<Option<OutputRoot>> Match(DiscogsApiClient discogsApiClient, TrackDirInfo dirInfo)
        {
            var matchingReleases = Arr<Release>.Empty;
            var queries = dirInfo.CommonPrefix.Bind(DirNameToQueries)
                .Concat(DirNameToQueries(Path.GetFileName(dirInfo.Path)));
            foreach (var q in queries)
            {
                if (!matchingReleases.IsEmpty) break;
                matchingReleases = await FindMatchingReleases(discogsApiClient, dirInfo, q, dirInfo.Tracks.Count);
            }

            if (matchingReleases.IsEmpty) return None;

            var canonicity = fun<string, int>(StringUtils.MeasureStringCanonicity);

            var sortedReleases = matchingReleases
                .OrderByDescending(x => (x.Tracklist.Select(y => y.Title).Sum(canonicity), x.Community.Have)).ToArray();
            var bestRelease = sortedReleases.First();

            var trackInfos = new List<MatchedTrackInfo>();
            foreach (var trackFileInfo in dirInfo.Tracks)
            {
                var releaseTrack = trackFileInfo.TrackNumber.Match(
                    n => sortedReleases.Map(x => x.Tracklist[n - 1]).OrderByDescending(x => canonicity(x.Title))
                        .First(),
                    () => bestRelease.Tracklist
                        .OrderBy(x => StringUtils.CalculateLevenshteinDistance(x.Title, trackFileInfo.TrackTitle))
                        .First()
                );
                var isReleaseTrackMoreCanonical =
                    canonicity(releaseTrack.Title) >= canonicity(trackFileInfo.TrackTitle);
                var isTrackTitleUseless =
                    trackFileInfo.MeaningfulTrackTitleTokens.Length * 3 < releaseTrack.Title.Length;
                var trackTitle = isReleaseTrackMoreCanonical || isTrackTitleUseless
                    ? releaseTrack.Title.Trim()
                    : trackFileInfo.TrackTitle;
                var trackNewNumber = trackFileInfo.TrackNumber.IfNone(() => int.Parse(releaseTrack.Position));

                trackInfos.Add(new MatchedTrackInfo(
                    path: Path.Combine(dirInfo.Path, trackFileInfo.FileName),
                    trackNumber: trackNewNumber,
                    title: trackTitle
                ));
            }

            var bestReleaseTitle = sortedReleases.Map(x => x.Title)
                .Append(Path.GetFileName(dirInfo.Path))
                .OrderByDescending(canonicity)
                .First();
            return new OutputRoot(
                album: bestReleaseTitle,
                performers: bestRelease.Artists.Map(x => x.Name).ToArr(),
                tracks: trackInfos.ToArr()
            );
        }
    }
}