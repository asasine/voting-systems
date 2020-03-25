using CommandLine;

namespace Vote.Configuration
{
    [Verb("bracket", HelpText = "Outputs pairs of head-to-head competitions for a bracket vote.")]
    public class BracketOptions : Options
    {
    }
}
