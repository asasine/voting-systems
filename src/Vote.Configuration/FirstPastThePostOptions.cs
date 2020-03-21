using CommandLine;

namespace Vote.Configuration
{
    [Verb("first-past-the-post", HelpText = "Uses first past the post voting, also known as a simple majority rule.")]
    public class FirstPastThePostOptions : Options
    {
    }
}
