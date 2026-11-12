using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    DbSet<Department> Departments { get; }
    DbSet<Suggestion> Suggestions { get; }
    DbSet<Template> Templates { get; }
}
