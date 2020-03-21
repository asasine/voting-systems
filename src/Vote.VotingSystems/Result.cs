using System;
using System.Collections.Generic;

namespace Vote.VotingSystems
{
    public class Result : IEquatable<Result>
    {
        public Result(Candidate candidate, int wins, int losses)
        {
            this.Candidate = candidate;
            this.Wins = wins;
            this.Losses = losses;
        }

        public Candidate Candidate { get; }
        public int Wins { get; }
        public int Losses { get; }
        public int Net => Wins - Losses;

        public override string ToString()
            => $"<{nameof(Result)}({this.Candidate}; {this.Net}={this.Wins}-{this.Losses})>";

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (object.ReferenceEquals(this, obj)) return true;
            if (!(obj is Result other)) return false;

            return Equals(other);
        }

        public bool Equals(Result other)
        {
            return other != null
                && this.Candidate.Equals(other.Candidate)
                && this.Wins == other.Wins
                && this.Losses == other.Losses
                && this.Net == other.Net;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Candidate, this.Wins, this.Losses, this.Net);
        }

        public static bool operator ==(Result left, Result right)
        {
            return EqualityComparer<Result>.Default.Equals(left, right);
        }

        public static bool operator !=(Result left, Result right)
        {
            return !(left == right);
        }
    }
}
