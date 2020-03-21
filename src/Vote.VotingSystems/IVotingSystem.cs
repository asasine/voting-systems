using System.Collections.Generic;
using System.Linq;

namespace Vote.VotingSystems
{
    public interface IVotingSystem
    {
        /// <summary>
        /// Returns an ordered collection of candidates with the winner as the first element and the loser as the last element.
        /// If the voting system cannot return a ranked collection, the result may only have a single element.
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="votes">The population's votes. Each individual's votes is ranked with the highest rank as the first element.</param>
        /// <returns>An ordered collection of candidates with the winner as the first element.</returns>
        IReadOnlyCollection<Result> GetRankedResults(ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes);
    }

    public static class VotingSystemExtensions
    {

        /// <summary>
        /// Returns the winner.
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="votes">The population's votes. Each individual's votes is ranked with the highest rank as the first element.</param>
        /// <returns>The winner.</returns>
        public static Result GetWinner(this IVotingSystem votingSystem, ISet<Candidate> candidates, IEnumerable<IEnumerable<Candidate>> votes)
            => votingSystem.GetRankedResults(candidates, votes).FirstOrDefault();
    }
}