using System;
using Discogs.ApiClient;
using Murg.Backend.Cache;

namespace Murg.Backend
{
    public static class DiscogsApiClientProvider
    {
        static DiscogsApiClientEnvironment CreateDiscogsApiClientEnvironment(
            DiscogsApiClientCredentials credentials
        )
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));

            var httpClient = new CachingHttpClient { BaseAddress = new Uri("https://api.discogs.com/") };
            var headers = httpClient.DefaultRequestHeaders;
            headers.Add("User-Agent", "Murg");
            headers.Add("Authorization", $"Discogs token={credentials.Token}");

            return new DiscogsApiClientEnvironment(credentials, httpClient);
        }

        public static DiscogsApiClient CreateDiscogsApiClient()
        {
            var creds = DiscogsApiClientCredentials.FromToken("lfLtpaRUcabAukAndQmtrnlLRSVRkemAGzekIYdk");
            var discogsApiClientEnvironment = CreateDiscogsApiClientEnvironment(creds);
            var client = DiscogsApiClient.Create(discogsApiClientEnvironment);
            return client;
        }
    }
}