using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vote.Api.IMDb;

namespace Vote.VotingSystems
{
    public class Bracket : IVotingSystem
    {
        private readonly ILogger logger;
        private readonly IMDbApiService apiService;

        public Bracket(ILogger<Bracket> logger, IMDbApiService apiService)
        {
            this.logger = logger;
            this.apiService = apiService;
        }

        public async Task<IReadOnlyCollection<Result>> GetRankedResultsAsync(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
        {
            // voting not actually implemented for this method
            // it just outputs the brackets using the apiService for sorting

            var count = candidates.Count();
            if ((count & (count - 1)) != 0)
            {
                this.logger.LogError("Cannot process brackets for {count} candidates, count must be a power of 2", count);
                throw new ArgumentException($"Cannot process brackets for {count} candidates, count must be a power of 2", nameof(candidates));
            }

            var searchTasks = candidates
                .Select(async candidate => (candidate, result: await this.apiService.SearchForTitleAsync(candidate.Name)));

            var searchResults = await Task.WhenAll(searchTasks);
            var ids = searchResults
                .Select(pair =>
                {
                    var result = pair.result.results.First();
                    this.logger.LogDebug("From {candidate} selecting {title} with id {id}", pair.candidate, result.title, result.id);
                    return (pair.candidate, result.id);
                });

            var ratingTasks = ids
                .Select(async pair =>
                {
                    var result = await this.apiService.GetRatingsAsync(pair.id);
                    var r = result.imDb;
                    if (string.IsNullOrWhiteSpace(r) || !double.TryParse(r, out double rating))
                    {
                        return (pair.candidate, rating: 5.0);
                    }
                    else
                    {
                        return (pair.candidate, rating);
                    }
                });

            var ratings = await Task.WhenAll(ratingTasks);
            var ratingsLookup = ratings.ToDictionary(pair => pair.candidate, pair => pair.rating);
            var ordered = candidates
                .Select((candidate, i) => (candidate, rating: ratingsLookup.GetValueOrDefault(candidate)))
                .OrderByDescending(r => r.rating)
                .Select((r, i) => (r.candidate, r.rating, seed: i + 1))
                .ToList();

            var highest = ordered
                .Take(count / 2);

            var lowest = ordered
                .Skip(count / 2)
                .Reverse();

            // 1v32, 2v31, 3v30, ..., 15v18, 16v17
            var pairs = highest.Zip(lowest, (lhs, rhs) => (lhs, rhs));
            foreach (var (lhs, rhs) in pairs)
            {
                var lhsCandidate = lhs.candidate.Name;
                var lhsSeed = lhs.seed;
                var lhsRating = lhs.rating;
                var rhsCandidate = rhs.candidate.Name;
                var rhsSeed = rhs.seed;
                var rhsRating = rhs.rating;
                this.logger.LogInformation("#{lhsSeed} {lhsCandidate} ({lhsRating}) vs #{rhsSeed} {rhsCandidate} ({rhsRating})", lhsSeed, lhsCandidate, lhsRating, rhsSeed, rhsCandidate, rhsRating);
            }

            return new Result[0];
        }
    }
}
