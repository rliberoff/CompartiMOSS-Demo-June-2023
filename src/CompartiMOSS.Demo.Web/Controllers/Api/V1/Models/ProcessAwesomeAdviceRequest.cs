using System.ComponentModel.DataAnnotations;

using CompartiMOSS.Demo.Web.Infrastructure;

namespace CompartiMOSS.Demo.Web.Controllers.Api.V1.Models;

public sealed class ProcessAwesomeAdviceRequest
{
    [Required]
    [NotEmptyOrWhitespace]
    public string Input { get; init; }
}
