using JobRag.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRag.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.CountryCode)
            .IsRequired()
            .HasMaxLength(5)
            .HasDefaultValue("DE");

        builder.Property(p => p.CVText)
            .HasColumnType("text");

        builder.Property(p => p.CVEmbedding)
            .HasColumnType("vector(1536)");

        builder.Property(p => p.PreferencesJson)
            .HasColumnType("jsonb");

        builder.Property(p => p.UpdatedAt)
            .HasDefaultValueSql("NOW()");
    }
}
