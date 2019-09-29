using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LanguageExt;
using Murg.Backend.Input;
using static LanguageExt.Prelude;

namespace Murg.Backend.InfoExtraction
{
    public static class TrackDirInfoExtractor
    {
        static TrackInfo ExtractTrackInfo(string originalFileName, HashSet<string> commonTokens, string fileName)
        {
            string RemoveCommonTokens(string s) => s
                .Apply(StringUtils.SplitToTokens)
                .Filter(x => !commonTokens.Contains(x))
                .Apply(ws => string.Join(' ', ws));
            
            var trackNoRegex = new Regex("(?<n>\\d+) ?[-.] ?");
            var match = trackNoRegex.Matches(fileName).LastOrDefault();

            if (match == null) return new TrackInfo(
                fileName: originalFileName,
                trackNumber: None, 
                trackTitle: fileName, 
                meaningfulTrackTitleTokens: RemoveCommonTokens(fileName)
            );
            var trackNumber = int.Parse(match.Groups["n"].Value);
            var trackTitle = fileName.Substring(match.Index + match.Length).Trim();
            commonTokens = commonTokens.TryAdd(trackNumber.ToString("0")).TryAdd(trackNumber.ToString("00"));
            return new TrackInfo(
                fileName: originalFileName, 
                trackNumber: trackNumber, 
                trackTitle: trackTitle,
                meaningfulTrackTitleTokens: RemoveCommonTokens(trackTitle)
            );
        }
        
        public static TrackDirInfo Extract(InputRoot input)
        {
            var path = input.Uri.LocalPath; // TODO
            var files = input.Files.Filter(x => x.Type == InputFileType.Audio).OrderBy(x => x.Path).ToList();
            if (files.Count == 0) return new TrackDirInfo(path: path, commonPrefix: None, tracks: Empty);
            
            var prefix = files
                .Map(x => x.Path)
                .Map(Path.GetFileNameWithoutExtension)
                .Apply(StringUtils.FindLongestCommonPrefix(ignoreCase: true))
                .Apply(x => x.Trim());
            var commonTokens = files
                .Map(x => x.Path)
                .Map(Path.GetFileNameWithoutExtension)
                .Map(s => s.Substring(prefix.Length))
                .Apply(StringUtils.FindCommonTokens)
                .Apply(toHashSet);
            
            var tracks = files.Map(file =>
            {
                var filePath = file.Path;
                var originalFileName = Path.GetFileName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath).Substring(prefix.Length).Trim();
                return ExtractTrackInfo(originalFileName, commonTokens, fileName);
            }).ToArr();
            return new TrackDirInfo(path: path, commonPrefix: prefix.Apply(Some).Filter(x => x.Length > 0), tracks: tracks);
        }
    }
}