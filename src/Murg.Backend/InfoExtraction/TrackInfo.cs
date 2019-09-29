using LanguageExt;

namespace Murg.Backend.InfoExtraction
{
    public sealed class TrackInfo
    {
        public readonly string FileName;
        public readonly Option<int> TrackNumber;
        public readonly string TrackTitle;
        public readonly string MeaningfulTrackTitleTokens;

        public TrackInfo(Some<string> fileName, Option<int> trackNumber, Some<string> trackTitle, Some<string> meaningfulTrackTitleTokens)
        {
            FileName = fileName;
            TrackNumber = trackNumber;
            TrackTitle = trackTitle;
            MeaningfulTrackTitleTokens = meaningfulTrackTitleTokens;
        }
    }
}