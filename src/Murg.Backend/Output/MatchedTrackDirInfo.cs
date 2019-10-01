using LanguageExt;
using Murg.Backend.InfoExtraction;

namespace Murg.Backend.Output
{
    public sealed class MatchedTrackDirInfo
    {
        public readonly string Path;
        public readonly Option<string> CommonPrefix;
        public readonly Arr<InfoExtraction.TrackInfo> Tracks;

        public MatchedTrackDirInfo(string path, Option<string> commonPrefix, Arr<InfoExtraction.TrackInfo> tracks)
        {
            Path = path;
            CommonPrefix = commonPrefix;
            Tracks = tracks;
        }
    }
}