using System.Linq.Expressions;
using KaizenBoard.Application.Common.Behaviours;
using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;
using KaizenBoard.Application.UnitTests.TestDoubles;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace KaizenBoard.Application.UnitTests.Suggestions;

/// <summary>
/// Тесты: PUT /api/Suggestions/{id} — проверка версии записи.
/// Ожидаемые коды HTTP (см. ProblemDetailsExceptionHandler):
///   нет версии        → ValidationException   → 400
///   версия устарела   → InvalidOperationException → 409
///   всё верно         → 200 + новая версия
/// </summary>
[TestFixture]
public class UpdateSuggestionCommandHandlerTests
{
    private const string CurrentUserId = "user-42";

    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IUser> _currentUser = null!;
    private UpdateSuggestionCommandHandler _handler = null!;

    private Suggestion _suggestion = null!;

    [SetUp]
    public void SetUp()
    {
        _suggestion = new Suggestion
        {
            Id = Guid.NewGuid(),
            CreatedBy = CurrentUserId,
            Created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastModified = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
            Title = "Переписать БД на PostgreSQL",
            CurrentFlaws = "База устарела",
            ProposedSolution = "Мигрировать",
            ImprovementObject = "База данных",
            TargetDepartmentId = Guid.NewGuid(),
            Status = SuggestionStatus.Draft,
        };

        _currentUser = new Mock<IUser>();
        _currentUser.SetupGet(u => u.Id).Returns(CurrentUserId);

        _context = new Mock<IApplicationDbContext>();
        _context.Setup(c => c.Suggestions).Returns(CreateDbSetMock(_suggestion));
        _context.Setup(c => c.Departments).Returns(CreateDbSetMock(new Department
        {
            Id = _suggestion.TargetDepartmentId,
            Name = "ИТ",
        }));
        _context
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new UpdateSuggestionCommandHandler(_context.Object, _currentUser.Object);
    }

    // ------------------------------------------------------------------
    // 400: версии нет вообще
    // ------------------------------------------------------------------
    [TestCase("", TestName = "400: версия — пустая строка")]
    [TestCase("   ", TestName = "400: версия — только пробелы")]
    public async Task Validator_WithoutVersion_ThrowsValidationException(string version)
    {
        // Пустую версию ловит валидатор команды, а ValidationBehaviour
        // превращает это в ValidationException → 400 (как в контейнере).
        var command = CreateCommand(version);
        var behaviour = new ValidationBehaviour<UpdateSuggestionCommand, string>(
            new FluentValidation.IValidator<UpdateSuggestionCommand>[] { new UpdateSuggestionCommandValidator() });

        var ex = await Should.ThrowAsync<ValidationException>(
            () => behaviour.Handle(command, _ => Task.FromResult(string.Empty), CancellationToken.None));

        ex.Errors.ShouldContainKey("Version");
        ex.Errors["Version"][0].ShouldContain("Версия записи обязательна");
    }

    // ------------------------------------------------------------------
    // 409: версия есть, но не совпадает с текущей (запись уже изменена)
    // ------------------------------------------------------------------
    [Test]
    public async Task Handle_WithStaleVersion_ThrowsInvalidOperationException()
    {
        // Какую версию "видит" клиент: считаем ДО правки, а в БД уже другие данные.
        var staleVersion = SuggestionVersion.Compute(_suggestion);
        _suggestion.Title = "Кто-то другой уже изменил заголовок"; // запись изменилась после чтения

        var command = CreateCommand(staleVersion);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        ex.Message.ShouldContain("Запись уже изменена");
    }

    // ------------------------------------------------------------------
    // 200: версия совпала — правка применяется, возвращается НОВАЯ версия
    // ------------------------------------------------------------------
    [Test]
    public async Task Handle_WithCurrentVersion_UpdatesSuggestion_AndReturnsNewVersion()
    {
        var versionBefore = SuggestionVersion.Compute(_suggestion);
        var command = CreateCommand(versionBefore);
        command.Title = "Переписать БД на ClickHouse";

        var newVersion = await _handler.Handle(command, CancellationToken.None);

        _suggestion.Title.ShouldBe("Переписать БД на ClickHouse");
        newVersion.ShouldNotBe(versionBefore);           // версия сменилась после правки
        newVersion.ShouldBe(SuggestionVersion.Compute(_suggestion));
        _context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ------------------------------------------------------------------
    // Вспомогательное
    // ------------------------------------------------------------------
    private UpdateSuggestionCommand CreateCommand(string version) => new()
    {
        Id = _suggestion.Id,
        Title = "Переписать БД на PostgreSQL",
        CurrentFlaws = "База устарела",
        ProposedSolution = "Мигрировать",
        ImprovementObject = "База данных",
        TargetDepartmentId = _suggestion.TargetDepartmentId,
        TemplateId = null,
        Version = version,
    };

    private static DbSet<T> CreateDbSetMock<T>(params T[] items) where T : class
    {
        var queryable = items.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return mockSet.Object;
    }
}
