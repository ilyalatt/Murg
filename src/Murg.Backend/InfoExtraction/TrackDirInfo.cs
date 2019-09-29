using LanguageExt;

namespace Murg.Backend.InfoExtraction
{
    public sealed class TrackDirInfo
    {
        public readonly string Path;
        public readonly Option<string> CommonPrefix;
        public readonly Arr<TrackInfo> Tracks;

        public TrackDirInfo(string path, Option<string> commonPrefix, Arr<TrackInfo> tracks)
        {
            Path = path;
            CommonPrefix = commonPrefix;
            Tracks = tracks;
        }
    }
}