using System.ComponentModel.DataAnnotations;

namespace CompartiMOSS.Demo.Web.Infrastructure;

/// <summary>
/// Provides <see cref="Uri"/> validation.
/// </summary>
/// <remarks>
/// This is an alternative to <see cref="UrlAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class UriAttribute : DataTypeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UriAttribute"/> class.
    /// </summary>
    public UriAttribute() : base(DataType.Url)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see langword="null"/> is allowed.
    /// </summary>
    public bool AllowsNull { get; set; } = true;

    /// <inheritdoc/>
    public override bool IsValid(object value)
    {
        return (AllowsNull && value == null) || (value is Uri uri && uri.IsWellFormedOriginalString());
    }
}
