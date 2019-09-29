using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Murg.Backend.FsInfo
{
    public static class TrackDirInfoExtractor
    {
        static List<string> GetAudioFiles(string dir)
        {
            var extensions = new[] { ".mp3", ".ape", ".flac", ".mpc", ".ogg", ".wav" };
            return Directory.GetFiles(dir).Filter(x => extensions.Contains(Path.GetExtension(x)?.ToLower())).ToList();
        }

        static TrackInfo ExtractTrackInfo(string originalFileName, LanguageExt.HashSet<string> commonTokens, string fileName)
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
        
        public static TrackDirInfo Extract(string path)
        {
            var filePaths = GetAudioFiles(path).OrderBy(x => x).ToList();
            if (filePaths.Count == 0) return new TrackDirInfo(path: path, commonPrefix: None, tracks: Empty);
            
            var prefix = filePaths
                .Map(Path.GetFileNameWithoutExtension)
                .Apply(StringUtils.FindLongestCommonPrefix(ignoreCase: true))
                .Apply(x => x.Trim());
            var commonTokens = filePaths
                .Map(Path.GetFileNameWithoutExtension)
                .Map(s => s.Substring(prefix.Length))
                .Apply(StringUtils.FindCommonTokens)
                .Apply(toHashSet);
            
            var tracks = filePaths.Map(filePath =>
            {
                var originalFileName = Path.GetFileName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath).Substring(prefix.Length).Trim();
                return ExtractTrackInfo(originalFileName, commonTokens, fileName);
            }).ToArr();
            return new TrackDirInfo(path: path, commonPrefix: prefix.Apply(Some).Filter(x => x.Length > 0), tracks: tracks);
        }
    }
}