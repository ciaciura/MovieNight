using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Models.Persistence;

namespace Infrastructure.Data.Configurations;

public sealed class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.ToTable("users");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DisplayName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(m => m.NormalizedDisplayName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(m => m.NormalizedDisplayName)
            .IsUnique();
    }
}
