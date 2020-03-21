using System;
using System.Collections.Generic;
using System.Linq;

namespace Vote.VotingSystems
{
    public class FirstPastThePost : IVotingSystem
    {
        public IReadOnlyCollection<Result> GetRankedResults(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
        {
            var seed = new Dictionary<Candidate, int>();
            var results = votes
                .Select(vote => vote.FirstOrDefault())
                .Where(candidate => candidate != default)
                .Aggregate(seed, func, resultSelector);

            return results;

            static Dictionary<Candidate, int> func(Dictionary<Candidate, int> tally, Candidate candidate)
            {
                // try and add the candidate with a count of 1 otherwise increment the candidate's count by 1
                if (!tally.TryAdd(candidate, 1))
                {
                    tally[candidate] += 1;
                }

                return tally;
            }

            static IReadOnlyCollection<Result> resultSelector(Dictionary<Candidate, int> tally)
                => tally
                    .OrderByDescending(pair => pair.Value)
                    .Select(pair => new Result(pair.Key, pair.Value, 0))
                    .ToList();
        }
    }
}
