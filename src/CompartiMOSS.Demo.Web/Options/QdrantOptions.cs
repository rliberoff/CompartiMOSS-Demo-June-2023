using System.ComponentModel.DataAnnotations;

using CompartiMOSS.Demo.Web.Infrastructure;

namespace CompartiMOSS.Demo.Web.Options;

/// <summary>
/// Configuration options for setting up a connection to Qdrant vector database.
/// </summary>
internal sealed class QdrantOptions
{
    /// <summary>
    /// Gets the endpoint protocol and host (e.g. http://localhost).
    /// </summary>
    [Required]
    [Uri]
    public Uri Host { get; init; }

    /// <summary>
    /// Gets the endpoint port.
    /// </summary>
    [Range(0, 65535)]
    [Required]
    public int Port { get; init; }

    /// <summary>
    /// Gets the vector size.
    /// </summary>
    [Range(1, int.MaxValue)]
    [Required]
    public int VectorSize { get; init; }
}
