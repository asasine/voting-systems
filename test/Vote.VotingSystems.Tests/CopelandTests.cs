using System.Linq;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Vote.VotingSystems.Tests
{
    public class CopelandTests
    {
        [Fact]
        public void Wikipedia_ABCDE()
        {
            var candidates = new string[] { "A", "B", "C", "D", "E", }
                .Select(c => new Candidate(c))
                .ToHashSet();

            var votes = Enumerable.Repeat(new string[] { "A", "E", "C", "D", "B", }, 31)
                    .Concat(Enumerable.Repeat(new string[] { "B", "A", "E", }, 30))
                    .Concat(Enumerable.Repeat(new string[] { "C", "D", "B", }, 29))
                    .Concat(Enumerable.Repeat(new string[] { "D", "A", "E" }, 10))
                .Select(votes => votes.Select(vote => new Candidate(vote)));

            var expectedResults = new Result[]
            {
                new Result(new Candidate("A"), 3, 1),
                new Result(new Candidate("B"), 2, 2),
                new Result(new Candidate("C"), 2, 2),
                new Result(new Candidate("E"), 2, 2),
                new Result(new Candidate("D"), 1, 3),
            };

            var copeland = new Copeland();
            var actualResults = copeland.GetRankedResults(candidates, votes);

            Assert.Equal(expectedResults.Count(), actualResults.Count());
            foreach (var (expected, actual) in expectedResults.Zip(actualResults))
            {
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Wikipedia_Tennessee()
        {
            var memphis = new Candidate("Memphis");
            var nashville = new Candidate("Nashville");
            var chattanooga = new Candidate("Chattanooga");
            var knoxville = new Candidate("Knoxville");

            var candidates = new Candidate[] { memphis, nashville, chattanooga, knoxville, }
                .ToHashSet();

            var votes = Enumerable.Repeat(new Candidate[] { memphis, nashville, chattanooga, knoxville, }, 42)
                    .Concat(Enumerable.Repeat(new Candidate[] { nashville, chattanooga, knoxville, memphis, }, 26))
                    .Concat(Enumerable.Repeat(new Candidate[] { chattanooga, knoxville, nashville, memphis, }, 15))
                    .Concat(Enumerable.Repeat(new Candidate[] { knoxville, chattanooga, nashville, memphis, }, 17));

            var expectedResults = new Result[]
            {
                new Result(nashville, 3, 0),
                new Result(chattanooga, 2, 1),
                new Result(knoxville, 1, 2),
                new Result(memphis, 0, 3),
            };

            var copeland = new Copeland();
            var actualResults = copeland.GetRankedResults(candidates, votes);

            Assert.Equal(expectedResults.Count(), actualResults.Count());
            foreach (var (expected, actual) in expectedResults.Zip(actualResults))
            {
                Assert.Equal(expected, actual);
            }
        }
    }
}
