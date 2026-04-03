using JobRag.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRag.Infrastructure.Persistence.Configurations;

public class JobEmbeddingConfiguration : IEntityTypeConfiguration<JobEmbedding>
{
    public void Configure(EntityTypeBuilder<JobEmbedding> builder)
    {
        builder.ToTable("JobEmbeddings");

        builder.HasKey(e => e.JobId);

        builder.Property(e => e.Embedding)
            .IsRequired()
            .HasColumnType("vector(1536)");

        // IVFFlat index for fast cosine similarity search
        builder.HasIndex(e => e.Embedding)
            .HasMethod("ivfflat")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("idx_job_embeddings_vector");
    }
}
