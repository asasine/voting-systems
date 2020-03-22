using CommandLine;
using Microsoft.Extensions.Logging;

namespace Vote.Configuration
{
    public class Options
    {
        [Option("candidates-path", SetName = "real-data", Required = true, HelpText = "The path to a text file with candidates. Each candidate should be listed on its own line.")]
        public string CandidatesPath { get; set; }

        [Option("votes-directory", SetName = "real-data", Required = true, HelpText = "The directory containing text files with votes. Each file should be a list of candidates with each on its own line.")]
        public string VotesDirectory { get; set; }

        [Option("use-mock-data", SetName = "mock-data", Default = false, Required = true, HelpText = "True to use mock data.")]
        public bool UseMockData { get; set; }

        [Option("log-level", Default = LogLevel.Information, Required = false, HelpText = "Sets the output logging level.")]
        public LogLevel LogLevel{ get; set;}
    }
}
