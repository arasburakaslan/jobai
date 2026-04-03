using JobRag.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRag.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(j => j.CountryCode)
            .IsRequired()
            .HasMaxLength(5)
            .HasDefaultValue("DE");

        builder.Property(j => j.Industry)
            .HasMaxLength(128);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(j => j.Company)
            .HasMaxLength(256);

        builder.Property(j => j.Location)
            .HasMaxLength(256);

        builder.Property(j => j.Description)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(j => j.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(j => j.Source)
            .HasMaxLength(128);

        builder.Property(j => j.Currency)
            .HasMaxLength(10);

        builder.Property(j => j.LanguageRequired)
            .HasMaxLength(50);

        builder.Property(j => j.DescriptionHash)
            .HasMaxLength(128);

        builder.Property(j => j.EmbeddingPending)
            .HasDefaultValue(true);

        builder.Property(j => j.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(j => j.Url).IsUnique().HasDatabaseName("idx_jobs_url");
        builder.HasIndex(j => j.CountryCode).HasDatabaseName("idx_jobs_country");
        builder.HasIndex(j => j.EmbeddingPending).HasDatabaseName("idx_jobs_embedding_pending");
        builder.HasIndex(j => j.DescriptionHash).HasDatabaseName("idx_jobs_description_hash");
        builder.HasIndex(j => new { j.CountryCode, j.Industry }).HasDatabaseName("idx_jobs_country_industry");

        // Relationship
        builder.HasOne(j => j.Embedding)
            .WithOne(e => e.Job)
            .HasForeignKey<JobEmbedding>(e => e.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
