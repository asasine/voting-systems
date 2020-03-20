using System;
using System.Collections.Generic;

namespace Vote.VotingSystems
{
    public class Candidate : IEquatable<Candidate>, IComparable<Candidate>
    {
        public Candidate(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public int CompareTo(Candidate other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(this.Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (object.ReferenceEquals(this, obj)) return true;
            if (!(obj is Candidate other)) return false;

            return Equals(other);
        }

        public bool Equals(Candidate other)
        {
            return other != null &&
                    this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public static bool operator ==(Candidate left, Candidate right)
        {
            return EqualityComparer<Candidate>.Default.Equals(left, right);
        }

        public static bool operator !=(Candidate left, Candidate right)
        {
            return !(left == right);
        }

        public override string ToString() => $"<{nameof(Candidate)}({this.Name})>";
    }
}
