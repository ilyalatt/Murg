using System.IO;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Murg.Backend.Input
{
    public static class ExtensionsInfo
    {
        public static HashSet<string> AudioExtensions =
            HashSet(".mp3", ".ape", ".flac", ".mpc", ".ogg", ".wav");

        public static HashSet<string> TextExtensions =
            HashSet(".txt", ".log", ".cue");

        public static HashSet<string> UsefulExtensions =
            AudioExtensions + TextExtensions;


        static bool IsOneOfExtensions(HashSet<string> extensions, string filePath) =>
            Path.GetExtension(filePath)?.ToLower().Apply(extensions.Contains) ?? false;

        public static bool IsAudio(string filePath) =>
            IsOneOfExtensions(AudioExtensions, filePath);

        public static bool IsText(string filePath) =>
            IsOneOfExtensions(TextExtensions, filePath);

        public static bool IsUseful(string filePath) =>
            IsOneOfExtensions(UsefulExtensions, filePath);
    }
}