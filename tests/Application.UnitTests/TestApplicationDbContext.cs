using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.UnitTests;

public class TestApplicationDbContext : DbContext, IApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options) : base(options) { }

    // Обязательно добавь ВСЕ таблицы (DbSet), которые есть в твоем интерфейсе IApplicationDbContext
    public DbSet<Department> Departments { get; set; }
    public DbSet<Template> Templates { get; set; }

    public DbSet<Suggestion> Suggestions { get; set; }
    // Если в IApplicationDbContext есть Suggestion или другие, добавь их сюда тоже
}
