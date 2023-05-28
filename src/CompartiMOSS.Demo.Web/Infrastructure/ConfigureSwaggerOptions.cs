using Asp.Versioning.ApiExplorer;

using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace CompartiMOSS.Demo.Web.Infrastructure;

internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => ApiVersionDescriptionProvider = provider;

    public IApiVersionDescriptionProvider ApiVersionDescriptionProvider { get; }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var groupName in ApiVersionDescriptionProvider.ApiVersionDescriptions.Select(d => d.GroupName))
        {
            options.SwaggerDoc(groupName, new OpenApiInfo()
            {
                Title = Constants.AppTitle,
                Version = groupName,
            });
        }
    }
}
