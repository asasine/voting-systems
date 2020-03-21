using System;
using Xunit;

namespace Vote.VotingSystems.Tests
{
    public class CandidateTests
    {
        [Theory]
        [InlineData("A", "A")]
        [InlineData("A", "a")]
        [InlineData("a", "A")]
        [InlineData("hello world", "HeLLo woRLd")]
        [InlineData("A", "B")]
        public void Equals_CaseInsensitive(string expectedName, string actualName)
        {
            // set up
            var expected = new Candidate(expectedName);

            // act
            var actual = new Candidate(actualName);

            // assert
            if (StringComparer.OrdinalIgnoreCase.Equals(expectedName, actualName))
            {
                Assert.Equal(expectedName, actualName, ignoreCase: true);
                Assert.Equal(expected, actual);
                Assert.True(expected.Equals((Candidate)actual));
                Assert.True(expected.Equals((object)actual));
                Assert.True(expected == actual);
                Assert.False(expected != actual);

                // hashcode should always be the same for two objects that are equal
                Assert.Equal(expected.GetHashCode(), actual.GetHashCode());
            }
            else
            {
                Assert.NotEqual(expectedName, actualName, StringComparer.OrdinalIgnoreCase);
                Assert.NotEqual(expected, actual);
                Assert.False(expected.Equals((Candidate)actual));
                Assert.False(expected.Equals((object)actual));
                Assert.False(expected == actual);
                Assert.True(expected != actual);
                // hashcode is not guaranteed different for unequal objects
            }
        }

        [Theory]
        [InlineData("A", "A")]
        [InlineData("A", "a")]
        [InlineData("a", "A")]
        [InlineData("a", "a")]
        [InlineData("A", "B")]
        [InlineData("B", "A")]
        public void CompareTo_UsesName(string leftName, string rightName)
        {
            var left = new Candidate(leftName);
            var right = new Candidate(rightName);

            var nameComparison = StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            var candidateComparison = left.CompareTo(right);

            Assert.Equal(nameComparison, candidateComparison);
        }
    }
}
