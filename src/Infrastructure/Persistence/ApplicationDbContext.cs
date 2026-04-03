using JobRag.Domain.Jobs;
using JobRag.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace JobRag.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext.
/// Uses pgvector for embedding storage and pg_trgm for fuzzy search.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobEmbedding> JobEmbeddings => Set<JobEmbedding>();
    public DbSet<UserSavedJob> UserSavedJobs => Set<UserSavedJob>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enable Postgres extensions
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-set audit fields
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
