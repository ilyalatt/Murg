using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Murg.Backend.Input
{
    public static class FsReader
    {
        const int FileContentSizeLimitInBytes = 1024 * 1024 * 100; // 100 KB
        
        static async Task<Option<string>> ReadContent(string filePath, CancellationToken ct = default)
        {
            var fileSize = new FileInfo(filePath);
            if (fileSize.Length > FileContentSizeLimitInBytes) return None;

            var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            return content;
        }

        // right now it works with directories only
        // it is easier to have information about tracks in the same directory
        public static async Task<InputRoot> GetInputRoot(string dirPath, CancellationToken ct = default)
        {
            dirPath = Path.GetFullPath(dirPath);
            var files = Directory.GetFiles(dirPath);
            
            var inputFiles = await files
                .Filter(ExtensionsInfo.IsUseful)
                .Map(async filePath =>
                {
                    var type = ExtensionsInfo.IsAudio(filePath) ? InputFileType.Audio : InputFileType.Text;
                    var content = type == InputFileType.Text
                        ? await ReadContent(filePath, ct).ConfigureAwait(false)
                        : None;
                    return new InputFile(filePath, type, content);
                })
                .Apply(Task.WhenAll)
                .ConfigureAwait(false);
            return new InputRoot(new Uri(dirPath), inputFiles);
        }
    }
}