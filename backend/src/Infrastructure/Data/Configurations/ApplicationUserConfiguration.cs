using KaizenBoard.Domain.Entities;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne<Department>()
               .WithMany()
               .HasForeignKey(u => u.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
