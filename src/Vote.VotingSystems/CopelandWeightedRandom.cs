using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Vote.VotingSystems
{
    public class CopelandWeightedRandom : Copeland
    {
        private readonly Random random;

        public CopelandWeightedRandom(Random random)
        {
            this.random = random;
        }

        public override IReadOnlyCollection<Result> GetRankedResults(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
        {
            // result.Net values are in range [-n+1, n-1]
            var copelandResults = base.GetRankedResults(candidates, votes).ToList();

            // convert this to a discrete distribution then sample that distribution
            var n = candidates.Count();
            var probabilities = copelandResults
                .Select(result => result.Net)
                .Select(net => net + n)
                .ToList();


            var sum = probabilities.Sum();
            var distribution = copelandResults
                .Zip(probabilities, (result, probability) => (result, probability))
                .SelectMany(pair => Enumerable.Repeat(pair.result, pair.probability));

            // range [0, sum) (exclusive)
            var randomIndex = this.random.Next(sum);
            var winner = distribution.ElementAt(randomIndex);

            return new Result[] { winner };
        }

        public override Result GetWinner(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
            => this.GetRankedResults(candidates, votes).First();
    }
}
