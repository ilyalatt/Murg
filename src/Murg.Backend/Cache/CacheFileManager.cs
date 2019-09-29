using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Murg.Backend.Cache
{
    static class CacheFileManager
    {
        const string DirName = "Murg";
        static readonly string DirPath = Path.Combine(Path.GetTempPath(), DirName);

        static CacheFileManager()
        {
            if (!Directory.Exists(DirPath)) Directory.CreateDirectory(DirPath);
        }

        static string GetUrlPath(string url)
        {
            var urlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(url))
                .Replace('/', '_');
            return Path.Combine(DirPath, urlBase64);
        }
        
        public static async Task<Option<string>> GetHttpResponseBody(string url)
        {
            var path = GetUrlPath(url);
            return !File.Exists(path) ? None : Some(await File.ReadAllTextAsync(path));
        }

        public static async Task SetHttpResponseBody(string url, string body)
        {
            var path = GetUrlPath(url);
            await File.WriteAllTextAsync(path, body);
        }
    }
}