using JobRag.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRag.Infrastructure.Persistence.Configurations;

public class UserSavedJobConfiguration : IEntityTypeConfiguration<UserSavedJob>
{
    public void Configure(EntityTypeBuilder<UserSavedJob> builder)
    {
        builder.ToTable("UserSavedJobs");

        builder.HasKey(usj => usj.Id);
        builder.Property(usj => usj.Id).HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(usj => usj.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Saved");

        builder.Property(usj => usj.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(usj => new { usj.UserId, usj.JobId }).IsUnique().HasDatabaseName("idx_usersavedjobs_user_job");

        // Relationships
        builder.HasOne(usj => usj.User)
            .WithMany(u => u.SavedJobs)
            .HasForeignKey(usj => usj.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(usj => usj.Job)
            .WithMany()
            .HasForeignKey(usj => usj.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
