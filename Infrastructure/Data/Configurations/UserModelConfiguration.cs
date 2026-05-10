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

        builder.Property(m => m.TwoFactorMethod)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Email)
            .HasMaxLength(320);

        builder.Property(m => m.TotpSecret)
            .HasMaxLength(64);

        builder.HasIndex(m => m.NormalizedDisplayName)
            .IsUnique();

        builder.HasIndex(m => m.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL");
    }
}
