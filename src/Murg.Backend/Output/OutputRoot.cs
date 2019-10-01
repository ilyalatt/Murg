using LanguageExt;

namespace Murg.Backend.Output
{
    public sealed class OutputRoot
    {
        public readonly string Album;
        public readonly Arr<string> Performers;
        public readonly Arr<MatchedTrackInfo> Tracks;

        public OutputRoot(Some<string> album, Arr<string> performers, Arr<MatchedTrackInfo> tracks)
        {
            Album = album;
            Performers = performers;
            Tracks = tracks;
        }
    }
}