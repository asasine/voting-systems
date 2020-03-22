using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Vote.VotingSystems
{
    public class Copeland : IVotingSystem
    {
        private readonly ILogger logger;

        public Copeland(ILogger<Copeland> logger)
        {
            this.logger = logger;
        }

        public virtual IReadOnlyCollection<Result> GetRankedResults(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
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
                    .Where(candidate =>
                    {
                        var contains = candidates.Contains(candidate);
                        if (!contains)
                        {
                            this.logger.LogInformation("{candidate} is a write-in", candidate);
                        }

                        return contains;
                    })
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
                this.logger.LogTrace("{lhs} ({lhsWins}) vs {rhs} ({rhsWins})", lhs, lhsWins, rhs, rhsWins);
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
