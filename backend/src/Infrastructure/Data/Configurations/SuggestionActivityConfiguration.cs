using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class SuggestionActivityConfiguration : IEntityTypeConfiguration<SuggestionActivity>
{
    public void Configure(EntityTypeBuilder<SuggestionActivity> builder)
    {
        builder.HasOne<Suggestion>().WithMany(s => s.Activity).HasForeignKey(s => s.SuggestionId);
        builder.HasIndex(s => new { s.SuggestionId, s.OccurredAt });
    }
}
