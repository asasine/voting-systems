using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vote.Api.IMDb;
using Vote.Configuration;
using Vote.VotingSystems;

namespace Vote
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = GetServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();

            logger.LogDebug("hello world");

            const bool mock = true;
            var candidates = await GetCandidatesAsync(mock);
            var votes = await GetVotesAsync(mock);

            var votingSystem = serviceProvider.GetService<Copeland>();
            var results = votingSystem.GetResults(candidates, votes);
            foreach (var result in results)
            {
                logger.LogInformation("{result}", result);
                // Console.WriteLine($"{result.Candidate.Name} with {result.Net} net");
            }

            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        static IServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder
                        .AddConsole(options =>
                        {
                            options.IncludeScopes = false;
                        })
                        .SetMinimumLevel(LogLevel.Debug);
                })
                .AddConfiguration()
                .AddVotingSystems();

            serviceCollection
                .AddHttpClient<IMDbApiService>(c =>
                {
                    c.BaseAddress = new Uri("https://imdb-api.com/");
                });

            return serviceCollection
                .BuildServiceProvider();
        }

        static async Task<ISet<Candidate>> GetCandidatesAsync(bool mock)
        {
            if (mock)
            {
                return new string[] { "A", "B", "C", "D", "E", }
                    .Select(c => new Candidate(c))
                    .ToHashSet();
            }
            else
            {
                var candidates = await FilenameToCandidates("/Users/adam/Projects/Vote/output/ballot.txt");
                return candidates.ToHashSet();
            }
        }

        static async Task<IEnumerable<IEnumerable<Candidate>>> GetVotesAsync(bool mock)
        {
            if (mock)
            {
                return Enumerable.Repeat(new string[] { "A", "E", "C", "D", "B", }, 31)
                    .Concat(Enumerable.Repeat(new string[] { "B", "A", "E", }, 30))
                    .Concat(Enumerable.Repeat(new string[] { "C", "D", "B", }, 29))
                    .Concat(Enumerable.Repeat(new string[] { "D", "A", "E" }, 10))
                .Select(votes => votes.Select(vote => new Candidate(vote)));
            }
            else
            {
                var files = Directory.EnumerateFiles("/Users/adam/Projects/Vote/input/votes");
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
            return services
                .AddSingleton<Copeland>();
        }
    }
}
