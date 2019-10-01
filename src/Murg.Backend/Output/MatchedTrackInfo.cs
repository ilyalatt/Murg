using LanguageExt;

namespace Murg.Backend.Output
{
    public sealed class MatchedTrackInfo
    {
        public readonly string Path;
        public readonly int TrackNumber;
        public readonly string Title;

        public MatchedTrackInfo(Some<string> path, int trackNumber, Some<string> title)
        {
            Path = path;
            TrackNumber = trackNumber;
            Title = title;
        }
    }
}