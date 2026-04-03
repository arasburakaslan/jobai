using JobRag.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRag.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplications");

        builder.HasKey(ja => ja.Id);
        builder.Property(ja => ja.Id).HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(ja => ja.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(ja => ja.Notes)
            .HasColumnType("text");

        builder.Property(ja => ja.CoverLetterText)
            .HasColumnType("text");

        builder.Property(ja => ja.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(ja => new { ja.UserId, ja.JobId }).IsUnique().HasDatabaseName("idx_jobapplications_user_job");

        // Relationships
        builder.HasOne(ja => ja.User)
            .WithMany()
            .HasForeignKey(ja => ja.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ja => ja.Job)
            .WithMany()
            .HasForeignKey(ja => ja.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
