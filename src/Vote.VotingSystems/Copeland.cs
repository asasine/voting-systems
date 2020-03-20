using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Vote.VotingSystems
{
    public class Copeland
    {
        private readonly ILogger logger;

        public Copeland(ILogger<Copeland> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Returns an ordered collection of candidates with the winner as the first element and the loser as the last element.
        /// Each element's key is the candidate and the value is their net score.
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="votes">
        /// The population's votes.
        /// Each individual's votes is ranked with the highest rank as the first element.
        /// </param>
        /// <returns>An ordered collection of candidates with the winner as the first element.</returns>
        public IReadOnlyCollection<Result> GetResults(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
        {
            // AvB, AvC, AvD, AvE, BvC, BvD, BvE, CvD, etc.
            var pairs = candidates
                .SelectMany((candidate, i) => candidates
                    .Skip(i + 1)
                    .Select(other => (lhs: candidate, rhs: other)))
                .ToList();

            var wins = candidates.ToDictionary(candidate => candidate, _ => candidates.ToDictionary(candidate => candidate, _ => 0));
            foreach (var vote in votes)
            {
                var candidatesInThisVote = new HashSet<Candidate>();

                var voteWithoutWriteIns = vote
                    .Where(candidate => candidates.Contains(candidate))
                    .ToArray();

                for (int i = 0; i < voteWithoutWriteIns.Length; i++)
                {
                    // current beats everything else in this list
                    var current = voteWithoutWriteIns[i];
                    for (int j = i + 1; j < voteWithoutWriteIns.Length; j++)
                    {
                        var next = voteWithoutWriteIns[j];
                        wins[current][next] += 1;
                    }
                }

                // everything in this vote beats everything not in the list
                var candidatesNotInThisVote = candidates.Except(voteWithoutWriteIns);
                foreach (var voted in voteWithoutWriteIns)
                {
                    foreach (var notVoted in candidatesNotInThisVote)
                    {
                        wins[voted][notVoted] += 1;
                    }
                }
            }


            var netWins = candidates.ToDictionary(candidate => candidate, _ => 0);
            var netLosses = candidates.ToDictionary(candidate => candidate, _ => 0);
            foreach (var (lhs, rhs) in pairs)
            {
                var lhsWins = wins[lhs][rhs];
                var rhsWins = wins[rhs][lhs];
                if (lhsWins > rhsWins)
                {
                    // lhs wins, rhs loses
                    netWins[lhs] += 1;
                    netLosses[rhs] += 1;
                }
                else if (rhsWins > lhsWins)
                {
                    // rhs wins, lhs loses
                    netWins[rhs] += 1;
                    netLosses[lhs] += 1;
                }

                // else it's a tie
            }

            return candidates
                .Select(candidate => new Result(candidate, netWins[candidate], netLosses[candidate]))
                .OrderByDescending(result => result.Net)
                .ThenByDescending(result => result.Wins)
                .ThenBy(result => result.Losses)
                .ToList();
        }
    }
}
