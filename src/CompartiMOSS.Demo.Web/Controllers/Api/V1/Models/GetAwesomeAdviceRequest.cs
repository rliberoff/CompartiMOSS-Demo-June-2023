using System.ComponentModel.DataAnnotations;

using CompartiMOSS.Demo.Web.Infrastructure;

namespace CompartiMOSS.Demo.Web.Controllers.Api.V1.Models;

public class GetAwesomeAdviceRequest
{
    [Required]
    [NotEmptyOrWhitespace]
    public string Ask { get; init; }
}
