using JobRag.Application.Abstractions;
using JobRag.Domain.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobRag.Api.Controllers;

/// <summary>
/// CRUD endpoints for job management (read-only for users, write for crawlers/admin).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobRepository jobRepository, ILogger<JobsController> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get a job by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Job>> GetById(Guid id, CancellationToken ct)
    {
        var job = await _jobRepository.GetByIdAsync(id, ct);
        if (job is null)
            return NotFound();

        return Ok(job);
    }

    /// <summary>
    /// Get jobs pending embedding (admin/worker use).
    /// </summary>
    [HttpGet("pending-embedding")]
    public async Task<ActionResult<IReadOnlyList<Job>>> GetPendingEmbedding(
        [FromQuery] int batchSize = 50,
        CancellationToken ct = default)
    {
        var jobs = await _jobRepository.GetPendingEmbeddingAsync(batchSize, ct);
        return Ok(jobs);
    }
}
