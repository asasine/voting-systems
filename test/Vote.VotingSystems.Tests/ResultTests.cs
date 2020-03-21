using System.Collections.Generic;
using FakeItEasy;
using Xunit;

namespace Vote.VotingSystems.Tests
{
    public class ResultTests
    {
        public static IEnumerable<object[]> Equal_Candidate_TestCases()
        {
            yield return testCase("a", "a");
            yield return testCase("b", "b");
            yield return testCase("a", "b");

            object[] testCase(string candidateName, string resultCandidateName)
                => new object[] { new Candidate(candidateName), 0, 0, new Result(new Candidate(resultCandidateName), 0, 0), };
        }

        [Theory]
        [MemberData(nameof(Equal_Candidate_TestCases))]
        public void Equal_Candidate(Candidate actualCandidate, int actualWins, int actualLosses, Result expectedResult)
        {
            var actualResult = new Result(actualCandidate, actualWins, actualLosses);
            if (actualCandidate == expectedResult.Candidate)
            {
                Assert.Equal(expectedResult, actualResult);
                Assert.True(expectedResult.Equals((Result)actualResult));
                Assert.True(expectedResult.Equals((object)actualResult));
                Assert.True(expectedResult == actualResult);
                Assert.False(expectedResult != actualResult);
                Assert.Equal(expectedResult.GetHashCode(), actualResult.GetHashCode());
            }
            else
            {
                Assert.NotEqual(expectedResult, actualResult);
                Assert.False(expectedResult.Equals((Result)actualResult));
                Assert.False(expectedResult.Equals((object)actualResult));
                Assert.False(expectedResult == actualResult);
                Assert.True(expectedResult != actualResult);
            }
        }

        public static IEnumerable<object[]> Equal_Wins_TestCases()
        {
            yield return testCase(0, 0);
            yield return testCase(1, 1);
            yield return testCase(1, 2);

            object[] testCase(int actualWins, int expectedWins)
                => new object[] { new Candidate("a"), actualWins, 0, new Result(new Candidate("a"), expectedWins, 0), };
        }

        [Theory]
        [MemberData(nameof(Equal_Wins_TestCases))]
        public void Equal_Wins(Candidate actualCandidate, int actualWins, int actualLosses, Result expectedResult)
        {
            var actualResult = new Result(actualCandidate, actualWins, actualLosses);
            if (actualWins == expectedResult.Wins)
            {
                Assert.Equal(expectedResult, actualResult);
                Assert.True(expectedResult.Equals((Result)actualResult));
                Assert.True(expectedResult.Equals((object)actualResult));
                Assert.True(expectedResult == actualResult);
                Assert.False(expectedResult != actualResult);
                Assert.Equal(expectedResult.GetHashCode(), actualResult.GetHashCode());
            }
            else
            {
                Assert.NotEqual(expectedResult, actualResult);
                Assert.False(expectedResult.Equals((Result)actualResult));
                Assert.False(expectedResult.Equals((object)actualResult));
                Assert.False(expectedResult == actualResult);
                Assert.True(expectedResult != actualResult);
            }
        }

        public static IEnumerable<object[]> Equal_Losses_TestCases()
        {
            yield return testCase(0, 0);
            yield return testCase(1, 1);
            yield return testCase(1, 2);

            object[] testCase(int actualLosses, int expectedLosses)
                => new object[] { new Candidate("a"), 0, actualLosses, new Result(new Candidate("a"), 0, expectedLosses), };
        }

        [Theory]
        [MemberData(nameof(Equal_Losses_TestCases))]
        public void Equal_Losses(Candidate actualCandidate, int actualWins, int actualLosses, Result expectedResult)
        {
            var actualResult = new Result(actualCandidate, actualWins, actualLosses);
            if (actualLosses == expectedResult.Losses)
            {
                Assert.Equal(expectedResult, actualResult);
                Assert.True(expectedResult.Equals((Result)actualResult));
                Assert.True(expectedResult.Equals((object)actualResult));
                Assert.True(expectedResult == actualResult);
                Assert.False(expectedResult != actualResult);
                Assert.Equal(expectedResult.GetHashCode(), actualResult.GetHashCode());
            }
            else
            {
                Assert.NotEqual(expectedResult, actualResult);
                Assert.False(expectedResult.Equals((Result)actualResult));
                Assert.False(expectedResult.Equals((object)actualResult));
                Assert.False(expectedResult == actualResult);
                Assert.True(expectedResult != actualResult);
            }
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 0, 1)]
        [InlineData(0, 1, -1)]
        [InlineData(1, 1, 0)]
        [InlineData(2, 1, 1)]
        public void Net_FromWinsAndLosses(int expectedWins, int expectedLosses, int expectedNet)
        {
            var actualResult = new Result(A.Fake<Candidate>(), expectedWins, expectedLosses);
            var actualNet = actualResult.Net;

            Assert.Equal(expectedNet, actualNet);
        }
    }
}
