using JobRag.Application.Abstractions;
using JobRag.Domain.Jobs;
using JobRag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobRag.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Job entities with deduplication-aware queries.
/// </summary>
public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _db;

    public JobRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Jobs
            .Include(j => j.Embedding)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task<Job?> GetByUrlAsync(string url, CancellationToken ct = default)
    {
        return await _db.Jobs.FirstOrDefaultAsync(j => j.Url == url, ct);
    }

    public async Task<Job?> GetByDescriptionHashAsync(string descriptionHash, CancellationToken ct = default)
    {
        return await _db.Jobs.FirstOrDefaultAsync(j => j.DescriptionHash == descriptionHash, ct);
    }

    public async Task<IReadOnlyList<Job>> GetPendingEmbeddingAsync(int batchSize, CancellationToken ct = default)
    {
        return await _db.Jobs
            .Where(j => j.EmbeddingPending && j.Embedding == null)
            .OrderBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Job job, CancellationToken ct = default)
    {
        await _db.Jobs.AddAsync(job, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
