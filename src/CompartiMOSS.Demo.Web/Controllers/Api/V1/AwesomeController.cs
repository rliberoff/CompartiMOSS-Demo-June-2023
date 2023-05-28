using System.ComponentModel.DataAnnotations;

using CompartiMOSS.Demo.Web.Controllers.Api.V1.Models;
using CompartiMOSS.Demo.Web.Infrastructure;
using CompartiMOSS.Demo.Web.Options;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Options;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace CompartiMOSS.Demo.Web.Controllers.Api.V1;

/// <summary>
/// Provides endpoints to process awesome advices.
/// </summary>
[ApiController]
[Produces(@"application/json")]
[Route(@"api/[controller]")]
[Route($@"api/v{{{Constants.Versioning.QueryStringVersion}:apiVersion}}/[controller]")]
public class AwesomeController : ControllerBase
{
    private readonly IKernel kernel;
    private readonly ILogger<AwesomeController> logger;

    private readonly AwesomeSkillOptions awesomeSkillOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwesomeController"/> class.
    /// </summary>
    /// <param name="kernel">A valid instance of a <see cref="IKernel"/>.</param>
    /// <param name="awesomeSkillOptions">Configuration options for the Awesome Skill.</param>
    /// <param name="logger">A valid instance of a logger for this controller.</param>
    public AwesomeController(IKernel kernel, IOptions<AwesomeSkillOptions> awesomeSkillOptions, ILogger<AwesomeController> logger)
    {
        this.kernel = kernel;
        this.logger = logger;

        this.awesomeSkillOptions = awesomeSkillOptions.Value;
    }

    /// <summary>
    /// Retrieves an advice from its unique identifier.
    /// </summary>
    /// <param name="adviceId">The advice unique identifier.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to receive notice of cancellation.</param>
    /// <returns>A <see cref="Task"/> containing an <see cref="IActionResult"/> with the result of this operation.</returns>
    /// <response code="200">The advice.</response>
    /// <response code="400">If the request is invalid or poorly constructed.</response>
    [HttpGet(@"advice/{adviceId}")]
    [ActionName(nameof(RetrieveAwesomeAdviceAsync))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RetrieveAwesomeAdviceResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetrieveAwesomeAdviceAsync(string adviceId, CancellationToken cancellationToken)
    {
        try
        {
            var memoryResult = await kernel.Memory.GetAsync(Constants.Memories.Collections.AwesomeMemoryCollection, adviceId, cancellationToken: cancellationToken);

            return memoryResult == null
                ? NotFound()
                : Ok(new RetrieveAwesomeAdviceResponse()
                {
                    Advice = memoryResult.Metadata.Text,
                });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets an awesome advice for your asked question.
    /// </summary>
    /// <param name="question">The question to answer with an awesome advice.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to receive notice of cancellation.</param>
    /// <returns>A <see cref="Task"/> containing an <see cref="IActionResult"/> with the result of this operation.</returns>
    /// <response code="200">The advice for the question.</response>
    /// <response code="400">If the request is invalid or poorly constructed.</response>
    [HttpGet(@"advice/ask")]
    [ActionName(nameof(RetrieveAwesomeAdviceAsync))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetAwesomeAdviceResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAwesomeAdviceAsync([FromQuery][Required(AllowEmptyStrings = false)][NotEmptyOrWhitespace] string question, CancellationToken cancellationToken)
    {
        try
        {
            var searchResults = await kernel.Memory.SearchAsync(Constants.Memories.Collections.AwesomeMemoryCollection,
                                                               question,
                                                               awesomeSkillOptions.ResultsLimit,
                                                               awesomeSkillOptions.RelevanceScore,
                                                               cancellationToken: cancellationToken)
                                                   .ToListAsync(cancellationToken);

            var variables = new ContextVariables();
            variables.Set(@"input", question);
            variables.Set(@"advices", searchResults.Any() ? string.Join("\n\n", searchResults.Select(x => x.Metadata.Text)) : string.Empty);

            var resultContext = await kernel.RunAsync(variables, cancellationToken, kernel.Skills.GetFunction(nameof(Constants.Skills.AwesomeSkill), Constants.Skills.AwesomeSkill.Functions.FunctionAwesomeAdvise));

            return resultContext.ErrorOccurred
                ? throw new InvalidOperationException(resultContext.ToString())
                : Ok(new GetAwesomeAdviceResponse()
                {
                    Advice = resultContext.Result,
                });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Process an awesome advice.
    /// </summary>
    /// <param name="request">A request with an awesome advice to process.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to receive notice of cancellation.</param>
    /// <returns>A <see cref="Task"/> containing an <see cref="IActionResult"/> with the result of this operation.</returns>
    /// <response code="200"></response>
    /// <response code="400">If the request is invalid or poorly constructed.</response>
    [HttpPost(@"process")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessAwesomeAdviceAsync(ProcessAwesomeAdviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var adviceId = Guid.NewGuid().ToString();

            await kernel.Memory.SaveInformationAsync(Constants.Memories.Collections.AwesomeMemoryCollection, request.Input, adviceId, cancellationToken: cancellationToken);

            return CreatedAtAction(nameof(RetrieveAwesomeAdviceAsync), routeValues: new { adviceId }, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            throw;
        }
    }
}
