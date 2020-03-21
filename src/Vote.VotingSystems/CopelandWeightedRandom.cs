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
            // use the Alias method to sample from a discrete distribution
            // the discrete distribution is found by converting the net score of each result into a weight
            // copeland's methodd yields net scores in the range [-n+1, n-1] where n is the number of candidates
            // convert this to a discrete distribution of [1, 2*n-1]
            var copelandResults = base.GetRankedResults(candidates, votes).ToList();
            var n = candidates.Count();
            var probabilities = copelandResults
                .Select(result => result.Net)
                .Select(net => net + n)
                .ToList();

            // create the distribution by repeating each candidate X times, X being its weight
            // then sample that distribution by iterating through the distribution by a random amount
            // the easiest common denominator to find is the sum of all weights because if you divided
            //  each element in probabilities by the sum of all probabilities, the new sum would be exactly 1
            // potential performance improvement: find a smaller common denominator allowing for less iterations when sampling
            var sum = probabilities.Sum();
            var distribution = copelandResults
                .Zip(probabilities, (result, probability) => (result, probability))
                .SelectMany(pair => Enumerable.Repeat(pair.result, pair.probability));

            var randomIndex = this.random.Next(sum); // this is exclusive
            var winner = distribution.ElementAt(randomIndex);

            return new Result[] { winner };
        }

        public override Result GetWinner(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
            => this.GetRankedResults(candidates, votes).First();
    }
}
