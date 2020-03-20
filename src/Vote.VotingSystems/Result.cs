namespace Vote.VotingSystems
{
    public class Result
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
    }
}
