using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.RegistrationNumber).HasMaxLength(200);
        builder.Property(t => t.LastModified).IsConcurrencyToken();
        builder.HasIndex(s => s.RegistrationNumber).IsUnique();
        builder.Property(s => s.CreationKey).HasMaxLength(128);
        builder.Property(s => s.CreationPayloadHash).HasMaxLength(64);
        builder.HasIndex(s => new { s.CreatedBy, s.CreationKey }).IsUnique()
            .HasFilter("\"CreationKey\" IS NOT NULL");

        builder.Property(t => t.EconomicEffectAmount).HasPrecision(18, 2);
        builder.Property(t => t.ImplementationCost).HasPrecision(18, 2);
        builder.Property(t => t.RewardAmount).HasPrecision(18, 2);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.ImplementerUserId);

        builder.HasOne<Department>()
               .WithMany()
               .HasForeignKey(t => t.TargetDepartmentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
