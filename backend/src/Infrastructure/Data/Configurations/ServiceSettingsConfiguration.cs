using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class ServiceSettingsConfiguration : IEntityTypeConfiguration<ServiceSettings>
{
    public void Configure(EntityTypeBuilder<ServiceSettings> builder)
    {
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Version).IsConcurrencyToken();
    }
}
