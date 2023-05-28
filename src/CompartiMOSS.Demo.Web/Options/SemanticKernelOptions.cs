using System.ComponentModel.DataAnnotations;

using CompartiMOSS.Demo.Web.Infrastructure;

namespace CompartiMOSS.Demo.Web.Options;

internal sealed class SemanticKernelOptions
{
    [Required]
    [NotEmptyOrWhitespace]
    public string CompletionsModel { get; init; }

    [Required]
    [NotEmptyOrWhitespace]
    public string EmbeddingsModel { get; init; }

    [Required]
    [Uri]
    public Uri Endpoint { get; init; }

    [Required]
    [NotEmptyOrWhitespace]
    public string Key { get; init; }
}
