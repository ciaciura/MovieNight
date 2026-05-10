using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Models.Persistence;

namespace Infrastructure.Data.Configurations;

public sealed class TwoFactorOtpModelConfiguration : IEntityTypeConfiguration<TwoFactorOtpModel>
{
    public void Configure(EntityTypeBuilder<TwoFactorOtpModel> builder)
    {
        builder.ToTable("two_factor_otps");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(m => m.ExpiresAtUtc)
            .IsRequired();

        builder.Property(m => m.UsedAtUtc);

        builder.HasOne(m => m.User)
            .WithMany(u => u.TwoFactorOtps)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.UserId, m.UsedAtUtc });
    }
}
