using System.Reflection;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Infrastructure.Identity;
using KaizenBoard.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Suggestion> Suggestions => Set<Suggestion>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<ServiceSettings> ServiceSettings => Set<ServiceSettings>();
    public DbSet<SuggestionSupport> SuggestionSupports => Set<SuggestionSupport>();
    public DbSet<SuggestionActivity> SuggestionActivities => Set<SuggestionActivity>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
