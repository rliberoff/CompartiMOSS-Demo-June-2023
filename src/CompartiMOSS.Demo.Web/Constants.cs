namespace CompartiMOSS.Demo.Web;

internal static class Constants
{
    internal const string AppTitle = @"CompartiMOSS DEMO - Awesome Advisor";

    internal static class Memories
    {
        internal static class Collections
        {
            internal static readonly string AwesomeMemoryCollection = @"AwesomeMemoryCollection";
        }
    }

    internal static class Skills
    {
        internal static readonly string SkillsDirectory = @"Skills";

        internal static class AwesomeSkill
        {
            internal static class Functions
            {
                internal static readonly string FunctionAwesomeAdvise = @"AwesomeAdvise";
            }
        }
    }

    internal static class Versioning
    {
        internal const string VersionPrefix = @"v";

        internal const string QueryStringVersion = $@"api-version";

        internal const string HeaderVersion = $@"x-api-version";
    }
}
