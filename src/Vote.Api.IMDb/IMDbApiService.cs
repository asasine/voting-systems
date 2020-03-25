using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using Vote.Configuration;

namespace Vote.Api.IMDb
{
    public class IMDbApiService
    {
        public const string SearchPolicyKey = "searchApiPolicy";
        public const string RatingsPolicyKey = "ratingsApiPolicy";

        private readonly ILogger logger;
        private readonly Secrets.IMDbApi secrets;
        private readonly HttpClient httpClient;
        private readonly IAsyncPolicy<SearchResult> searchApiPolicy;
        private readonly IAsyncPolicy<RatingsResult> ratingsApiPolicy;

        public IMDbApiService(ILogger<IMDbApiService> logger, Secrets.IMDbApi secrets, HttpClient httpClient, IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            this.logger = logger;
            this.secrets = secrets;
            this.httpClient = httpClient;

            this.searchApiPolicy = policyRegistry.Get<IAsyncPolicy<SearchResult>>(SearchPolicyKey);
            this.ratingsApiPolicy = policyRegistry.Get<IAsyncPolicy<RatingsResult>>(RatingsPolicyKey);
        }

        public async Task<SearchResult> SearchForTitleAsync(string expression)
        {
            var context = new Context(expression)
            {
                ["logger"] = this.logger,
            };

            return await this.searchApiPolicy.ExecuteAsync(searchForTitleAsync, context);

            async Task<SearchResult> searchForTitleAsync(Context context)
            {
                var expression = context.OperationKey;
                this.logger.LogDebug("Searching for {expression}", expression);
                var uri = GetUri("SearchTitle", expression);
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsJsonAsync<SearchResult>();
                return result;
            }
        }

        public async Task<RatingsResult> GetRatingsAsync(string id)
        {
            var context = new Context(id)
            {
                ["logger"] = this.logger,
            };

            return await this.ratingsApiPolicy.ExecuteAsync(getRatingsAsync, context);

            async Task<RatingsResult> getRatingsAsync(Context context)
            {
                var id = context.OperationKey;
                this.logger.LogDebug("Getting ratings for {id}", id);
                var uri = GetUri("Ratings", id);
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsJsonAsync<RatingsResult>();
                return result;
            }
        }

        private Uri GetUri(string api, params string[] args)
        {
            return new Uri($"/en/API/{api}/{secrets.ApiKey}/{string.Join('/', args)}", UriKind.Relative);
        }

        public class SearchResult
        {
            public IEnumerable<SearchResultInner> results;

            public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);

            public class SearchResultInner
            {
                public string id;
                public string title;
                public string description;

                public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
            }
        }

        public class RatingsResult
        {
            public string imDbId;
            public string title;
            public string fullTitle;
            public string type;
            public string year;
            public string imDb;
            public string metacritic;
            public string theMovieDb;
            public string rottenTomatoes;
            public string tV_com;
            public string filmAffinity;

            public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    internal static class HttpExtensions
    {
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent httpContent)
        {
            var json = await httpContent.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
