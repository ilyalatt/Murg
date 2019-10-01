using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt.UnsafeValueAccess;

namespace Murg.Backend.Cache
{
    sealed class CachingHttpClient : HttpClient
    {
        static HttpClientHandler CreateHttpClientHandler() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        
        public CachingHttpClient() : base(CreateHttpClientHandler()) { }
        
        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri.ToString();
            var cachedResponseBody = await CacheFileManager.GetHttpResponseBody(url);
            if (cachedResponseBody.IsSome)
            {
                var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(cachedResponseBody.ValueUnsafe())
                };
                return httpResponse;
            }
            
            var resp = await base.SendAsync(request, cancellationToken);
            var content = await resp.Content.ReadAsStringAsync();
            await CacheFileManager.SetHttpResponseBody(url, content);
            await Task.Delay(1000, cancellationToken);
            return resp;
        }
    }
}