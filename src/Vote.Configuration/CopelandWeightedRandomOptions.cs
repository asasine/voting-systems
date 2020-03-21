using CommandLine;

namespace Vote.Configuration
{
    [Verb("copeland-weighted-random", HelpText = "Uses Copeland's method to weight candidates then randomly selects a candidate.")]
    public class CopelandWeightedRandomOptions : CopelandOptions
    {
    }
}
