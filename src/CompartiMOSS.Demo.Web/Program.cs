using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Asp.Versioning;
using Asp.Versioning.Conventions;

using Azure.Identity;

using CompartiMOSS.Demo.Web;
using CompartiMOSS.Demo.Web.Infrastructure;
using CompartiMOSS.Demo.Web.Options;

using Hellang.Middleware.ProblemDetails;

using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;

using Swashbuckle.AspNetCore.SwaggerGen;

/*
 *  Load Configuration
 */

var programType = typeof(Program);

var applicationName = programType.Assembly.GetName().Name;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    ApplicationName = applicationName,
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
});

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

var azureCredentials = new ChainedTokenCredential(new DefaultAzureCredential(), new EnvironmentCredential());

if (Debugger.IsAttached)
{
    builder.Configuration.AddJsonFile(@"appsettings.debug.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddJsonFile($@"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($@"appsettings.{Environment.UserName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables()
                     ;

// Load configuration from Azure App Configuration, and set Key Vault client for secrets...
var appConfigurationConnectionString = builder.Configuration.GetConnectionString(@"AppConfig");

if (!string.IsNullOrWhiteSpace(appConfigurationConnectionString))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigurationConnectionString)
               .ConfigureKeyVault(keyVault =>
               {
                   keyVault.SetCredential(azureCredentials);
               })
               .Select(KeyFilter.Any, LabelFilter.Null) // Load configuration values with no label
               .Select(KeyFilter.Any, applicationName) // Override with any configuration values specific to current application
               ;
    }, optional: false);
}

var isDevelopment = builder.Environment.IsDevelopment();

/*
 *  Logging Configuration
 */

if (isDevelopment)
{
    builder.Logging.AddConsole();

    if (Debugger.IsAttached)
    {
        builder.Logging.AddDebug();
    }
}

var applicationInsightsConnectionString = builder.Configuration[@"ApplicationInsights:ConnectionString"];

builder.Logging.AddApplicationInsights((telemetryConfiguration) => telemetryConfiguration.ConnectionString = applicationInsightsConnectionString, (_) => { })
               .AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Trace)
               ;

/*
 *  Options Configuration
 */

builder.Services.AddOptions<QdrantOptions>().Bind(builder.Configuration.GetSection(nameof(QdrantOptions))).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<SemanticKernelOptions>().Bind(builder.Configuration.GetSection(nameof(SemanticKernelOptions))).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AwesomeSkillOptions>().Bind(builder.Configuration.GetSection(nameof(AwesomeSkillOptions))).ValidateDataAnnotations().ValidateOnStart();

/*
 *  Services Configuration
 */

builder.Services.AddApplicationInsightsTelemetry(builder.Configuration)
                .AddHttpContextAccessor()
                .AddRouting()
                ;

/*
 *  MVC Configuration
 */

builder.Services.AddProblemDetails(options =>
                {
                    // Only include exception details in a development environment.
                    options.IncludeExceptionDetails = (_, _) => isDevelopment;

                    // Just in case, map 'NotImplementedException' to the '501 Not Implemented' HTTP status code.
                    options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

                    // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is
                    // why it's added last. If an exception other than any mapped before is thrown, this will handle it.
                    options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
                })
                .AddControllers(options =>
                {
                    options.RequireHttpsPermanent = true;
                    options.SuppressAsyncSuffixInActionNames = true;
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                })
                ;

/*
 *  API Versioning
 */

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = ApiVersion.Neutral;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader(Constants.Versioning.QueryStringVersion),
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader(Constants.Versioning.HeaderVersion));

    options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
})
.AddMvc(options =>
{
    options.Conventions.Add(new VersionByNamespaceConvention());
})
.AddApiExplorer(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = ApiVersion.Neutral;
    options.GroupNameFormat = @"'v'V";
})
;

/*
 *  OpenAPI (Swagger) Configuration
 */

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $@"{programType.Assembly.GetName().Name}.xml"));
    options.OperationFilter<SwaggerDefaultValues>();
})
;

/*
 *  Semantic Kernel Configuration
 */

// Add Memory...
builder.Services.AddSingleton<IMemoryStore>(sp =>
{
    var qdrantOptions = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
    return new QdrantMemoryStore(qdrantOptions.Host.AbsoluteUri, qdrantOptions.Port, qdrantOptions.VectorSize, sp.GetRequiredService<ILogger<QdrantMemoryStore>>());
});

// Add Kernel...
builder.Services.AddScoped(sp =>
{
    var logger = sp.GetRequiredService<ILogger<IKernel>>();
    var options = sp.GetRequiredService<IOptions<SemanticKernelOptions>>().Value;

    var kernel = new KernelBuilder().Configure(configuration =>
    {
        configuration.AddAzureTextCompletionService(options.CompletionsModel, options.Endpoint.AbsoluteUri, options.Key, logger: logger);
        configuration.AddAzureTextEmbeddingGenerationService(options.EmbeddingsModel, options.Endpoint.AbsoluteUri, options.Key, logger: logger);
    })
    .WithLogger(logger)
    .WithMemoryStorage(sp.GetRequiredService<IMemoryStore>())
    .Build();

    kernel.ImportSemanticSkillFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), Constants.Skills.SkillsDirectory), nameof(Constants.Skills.AwesomeSkill));

    return kernel;
});

/*
 *  Application Middleware Configuration
 */

var app = builder.Build();

if (isDevelopment)
{
    app.UseDeveloperExceptionPage()
       .UseSwagger()
       .UseSwaggerUI(options =>
       {
           foreach (var groupName in app.DescribeApiVersions().Select(d => d.GroupName))
           {
               options.DocumentTitle = $@"{Constants.AppTitle} - {groupName}";
               options.SwaggerEndpoint($@"/swagger/{groupName}/swagger.json", groupName);
           }

           options.RoutePrefix = string.Empty;
       });
}

app.UseDefaultFiles()
   .UseStaticFiles()
   .UseProblemDetails()
   .UseRouting()
   .UseAuthentication()
   .UseAuthorization()
   .UseEndpoints(endpoints =>
   {
       endpoints.MapControllers();
   })
   ;

app.Run();

