using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using Polly.Caching.Memory;
using Polly.Caching.Serialization.Json;
using Polly.Contrib.WaitAndRetry;
using Polly.Registry;
using StackExchange.Redis;
using Vote.Api.IMDb;
using Vote.Configuration;
using Vote.VotingSystems;

namespace Vote
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var returnCode = await Parser
            .Default
            .ParseArguments(args, LoadVerbs())
            .MapResult<Options, Task<ReturnCode>>(
                async options => await RunAsync(options),
                _ => Task.FromResult(ReturnCode.FailedToParseOptions));

            Console.WriteLine($"Exiting with code {(int)returnCode} ({returnCode})");
            return (int)returnCode;
        }

        static Type[] LoadVerbs()
            => Assembly
                .GetAssembly(typeof(Options))
                .GetTypes()
                .Where(type => type.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

        static async Task<ReturnCode> RunAsync(Options options)
        {
            var serviceProvider = GetServiceProvider(options);

            var logger = serviceProvider.GetService<ILogger<Program>>();

            var results = await RunVotingMethodAsync(options, serviceProvider);
            if (results == null)
            {
                return ReturnCode.UnknownVotingMethod;
            }

            foreach (var result in results)
            {
                logger.LogWarning("{result}", result);
            }

            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return ReturnCode.Success;
        }

        static async Task<IReadOnlyCollection<Result>> RunVotingMethodAsync(Options options, IServiceProvider serviceProvider)
        {
            IVotingSystem votingSystem;
            switch (options)
            {
                case CopelandWeightedRandomOptions _:
                    votingSystem = serviceProvider.GetService<CopelandWeightedRandom>();
                    break;
                case CopelandOptions _:
                    votingSystem = serviceProvider.GetService<Copeland>();
                    break;
                case FirstPastThePostOptions _:
                    votingSystem = serviceProvider.GetService<FirstPastThePost>();
                    break;
                case BracketOptions _:
                    votingSystem = serviceProvider.GetService<Bracket>();
                    break;
                default:
                    return null;
            }

            var candidates = await GetCandidatesAsync(options);
            var votes = await GetVotesAsync(options);

            var results = await votingSystem.GetRankedResultsAsync(candidates, votes);
            return results;
        }

        static IServiceProvider GetServiceProvider(Options options)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder
                        .AddConsole()
                        .SetMinimumLevel(options.LogLevel);
                })
                .AddConfiguration()
                .AddVotingSystems()
                .AddSingleton<Random>();


            serviceCollection
                .AddMemoryCache()
                .AddSingleton<MemoryCacheProvider>()
                .AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = "localhost";
                    options.InstanceName = Assembly.GetExecutingAssembly().GetName().Name;
                });

            serviceCollection.AddPolly();

            serviceCollection
                .AddHttpClient<IMDbApiService>(c =>
                {
                    c.BaseAddress = new Uri("https://imdb-api.com/");
                });

            // add options as the base type
            serviceCollection.AddSingleton(options);

            // add options as all of its runtime types
            var optionsType = typeof(Options);
            var type = options.GetType();
            while (type != optionsType)
            {
                serviceCollection.AddSingleton(options.GetType(), options);
                type = type.BaseType;
            }

            return serviceCollection
                .BuildServiceProvider();
        }

        static async Task<ISet<Candidate>> GetCandidatesAsync(Options options)
        {
            if (options.UseMockData)
            {
                return new string[] { "A", "B", "C", "D", "E", }
                    .Select(c => new Candidate(c))
                    .ToHashSet();
            }
            else
            {
                var candidates = await FilenameToCandidates(options.CandidatesPath);
                return candidates.ToHashSet();
            }
        }

        static async Task<IEnumerable<IEnumerable<Candidate>>> GetVotesAsync(Options options)
        {
            if (options.UseMockData)
            {
                return Enumerable.Repeat(new string[] { "A", "E", "C", "D", "B", }, 31)
                    .Concat(Enumerable.Repeat(new string[] { "B", "A", "E", }, 30))
                    .Concat(Enumerable.Repeat(new string[] { "C", "D", "B", }, 29))
                    .Concat(Enumerable.Repeat(new string[] { "D", "A", "E" }, 10))
                .Select(votes => votes.Select(vote => new Candidate(vote)));
            }
            else
            {
                var files = Directory.EnumerateFiles(options.VotesDirectory);
                var votes = await Task.WhenAll(files.Select(FilenameToCandidates));
                return votes.Select(vote => vote.ToList());
            }
        }

        static async Task<IEnumerable<Candidate>> FilenameToCandidates(string filename)
        {
            var lines = await File.ReadAllLinesAsync(filename);
            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .Select(line => new Candidate(line));
        }

        private enum ReturnCode
        {
            Success = 0,
            FailedToParseOptions = 1,
            UnknownVotingMethod = 2,
        }
    }

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            return services
                .AddSingleton<IConfiguration>(configurationBuilder)
                .AddSingleton(serviceProvider =>
                {
                    var configuration = serviceProvider.GetService<IConfiguration>();
                    return configuration.GetSection("IMDbApi").Get<Secrets.IMDbApi>();
                });
        }

        public static IServiceCollection AddVotingSystems(this IServiceCollection services)
        {
            var votingSystemType = typeof(IVotingSystem);
            var types = Assembly
                .GetAssembly(votingSystemType)
                .GetTypes()
                .Where(type => !(type.IsInterface || type.IsAbstract))
                .Where(type => votingSystemType.IsAssignableFrom(type))
                .ToArray();

            foreach (var type in types)
            {
                services.AddSingleton(type);
            }

            return services;
        }

        public static IServiceCollection AddPolly(this IServiceCollection services)
        {
            return services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>(serviceProvider =>
            {
                var registry = new PolicyRegistry();
                var serializerSettings = new JsonSerializerSettings();

                var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5, fastFirst: true);
                var redisTransientErrorPolicy = Policy
                    .Handle<RedisConnectionException>()
                    .Or<RedisTimeoutException>()
                    .WaitAndRetryAsync(
                        delay,
                        onRetry: (exception, currentDelat, context) =>
                        {
                            var logger = context["logger"] as ILogger;
                            logger?.LogCritical("Retrying {key} with delay {delay}", context.OperationKey, currentDelat);
                        });

                static string typedCacheKeyStrategy<T>(Context context)
                    => $"{typeof(T).FullName}-{context.OperationKey}";

                IAsyncPolicy<TResult> getCachePolicy<TResult>()
                {
                    var cacheProvider = serviceProvider.GetRequiredService<MemoryCacheProvider>().AsyncFor<TResult>();
                    var memoryCachePolicy = Policy.CacheAsync(
                        cacheProvider: cacheProvider,
                        ttl: TimeSpan.FromMinutes(5),
                        cacheKeyStrategy: typedCacheKeyStrategy<TResult>);

                    var distributedCacheTransientErrorRetryPolicy = redisTransientErrorPolicy.AsAsyncPolicy<TResult>();
                    var serializer = new JsonSerializer<TResult>(serializerSettings);
                    var distributedCache = serviceProvider
                        .GetRequiredService<IDistributedCache>()
                        .AsAsyncCacheProvider<string>()
                        .WithSerializer<TResult, string>(serializer);

                    // calls to distributed cache should handle and retry transient errors
                    var distributedCachePolicy = Policy.WrapAsync(
                                    distributedCacheTransientErrorRetryPolicy,
                                    Policy.CacheAsync(
                                        cacheProvider: distributedCache,
                                        ttl: TimeSpan.FromDays(7),
                                        cacheKeyStrategy: typedCacheKeyStrategy<TResult>,
                                        onCacheGet: (context, key) => { },
                                        onCacheMiss: (context, key) => { },
                                        onCachePut: (context, key) => { },
                                        onCacheGetError: (context, key, exception) =>
                                        {
                                            var logger = context["logger"] as ILogger;
                                            logger?.LogWarning("Failed to get key {key}: {ex}", key, exception);
                                        },
                                        onCachePutError: (context, key, exception) =>
                                        {
                                            var logger = context["logger"] as ILogger;
                                            logger?.LogWarning("Failed to put key {key}: {ex}", key, exception);
                                        }));

                    // check memory cache, then check distributed cache
                    var fullCachePolicy = Policy.WrapAsync(
                                    memoryCachePolicy,
                                    distributedCachePolicy);

                    return fullCachePolicy;
                }

                registry.Add(IMDbApiService.RatingsPolicyKey, getCachePolicy<IMDbApiService.RatingsResult>());
                registry.Add(IMDbApiService.SearchPolicyKey, getCachePolicy<IMDbApiService.SearchResult>());

                return registry;
            });
        }
    }
}
