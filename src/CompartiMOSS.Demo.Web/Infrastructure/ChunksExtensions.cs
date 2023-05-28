namespace CompartiMOSS.Demo.Web.Infrastructure;

internal static class ChunksExtensions
{
    internal static int Size(this IEnumerable<string> chunks, Func<string, int> lengthFunction)
    {
        return chunks.Sum(c => lengthFunction(c));
    }
}
