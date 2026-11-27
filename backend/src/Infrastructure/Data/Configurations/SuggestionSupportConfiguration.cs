using KaizenBoard.Domain.Entities;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class SuggestionSupportConfiguration : IEntityTypeConfiguration<SuggestionSupport>
{
    public void Configure(EntityTypeBuilder<SuggestionSupport> builder)
    {
        builder.HasKey(s => new { s.SuggestionId, s.UserId });
        builder.HasOne<Suggestion>().WithMany(s => s.Supports).HasForeignKey(s => s.SuggestionId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.UserId);
    }
}
