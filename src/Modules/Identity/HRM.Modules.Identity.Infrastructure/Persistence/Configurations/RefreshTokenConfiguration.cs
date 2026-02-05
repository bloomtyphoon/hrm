using HRM.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for RefreshToken.
///
/// Table: Identity.RefreshTokens
/// Primary Key: Id (GUID)
/// Indexes: Token (unique), (AccountType, AccountId), ExpiresAt
/// </summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        // AccountType: discriminator column
        builder.Property(rt => rt.AccountType)
            .IsRequired()
            .HasColumnType("TINYINT");

        // AccountId: references Accounts.Id
        builder.Property(rt => rt.AccountId)
            .IsRequired();

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("NVARCHAR(200)");

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(rt => rt.RevokedAt)
            .HasColumnType("DATETIME2");

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(50)
            .HasColumnType("NVARCHAR(50)");

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(200)
            .HasColumnType("NVARCHAR(200)");

        builder.Property(rt => rt.CreatedByIp)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("NVARCHAR(50)");

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500)
            .HasColumnType("NVARCHAR(500)");

        builder.Property(rt => rt.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // Indexes

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => new { rt.AccountType, rt.AccountId })
            .HasDatabaseName("IX_RefreshTokens_AccountType_AccountId");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        builder.HasIndex(rt => new { rt.AccountType, rt.AccountId, rt.RevokedAt, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_AccountType_AccountId_Active");

        // Ignore computed properties
        builder.Ignore(rt => rt.IsActive);
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.DomainEvents);
    }
}
