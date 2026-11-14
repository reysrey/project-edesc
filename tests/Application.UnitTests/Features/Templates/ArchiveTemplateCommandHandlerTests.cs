using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Features.Templates.Commands.ArchiveTemplate;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace KaizenBoard.Application.UnitTests.Features.Templates;

[TestFixture]
public class ArchiveTemplateCommandHandlerTests
{
    // ВАЖНО: Используем конкретный класс TestApplicationDbContext
    private TestApplicationDbContext _context;
    private ArchiveTemplateCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestApplicationDbContext(options);
        _handler = new ArchiveTemplateCommandHandler(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task Handle_Should_Archive_Template_And_SaveChanges()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        _context.Templates.Add(new Template
        {
            Id = templateId,
            IsActive = true,
            Title = "Тестовый шаблон",
            Type = TemplateType.Template // Или строка, если у вас так
        });
        await _context.SaveChangesAsync();

        var command = new ArchiveTemplateCommand { Id = templateId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(templateId));

        var templateInDb = await _context.Templates.FindAsync(templateId);
        Assert.That(templateInDb!.IsActive, Is.False, "Шаблон должен быть заархивирован (IsActive = false)");
    }

    [Test]
    public async Task Handle_Should_Throw_NotFoundException_When_Template_Does_Not_Exist()
    {
        // Arrange
        var command = new ArchiveTemplateCommand { Id = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await _handler.Handle(command, CancellationToken.None));
    }
}
