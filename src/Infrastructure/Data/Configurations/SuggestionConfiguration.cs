using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.RegistrationNumber).HasMaxLength(200);
        builder.HasIndex(s => s.RegistrationNumber).IsUnique();

        builder.Property(t => t.EconomicEffectAmount).HasPrecision(18, 2);
        builder.Property(t => t.ImplementationCost).HasPrecision(18, 2);
        builder.Property(t => t.RewardAmount).HasPrecision(18, 2);

        builder.HasOne<Department>()
               .WithMany()
               .HasForeignKey(t => t.TargetDepartmentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
