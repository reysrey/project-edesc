using FluentValidation.TestHelper;
using KaizenBoard.Application.Features.Departments.Commands.UpdateDepartment;
using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace KaizenBoard.Application.UnitTests.Features.Departments;

[TestFixture]
public class UpdateDepartmentCommandValidatorTests
{
    // ВАЖНО: Используем конкретный класс TestApplicationDbContext, 
    // чтобы были доступны методы SaveChanges и Dispose
    private TestApplicationDbContext _context;
    private UpdateDepartmentCommandValidator _validator;

    [SetUp]
    public void Setup()
    {
        // 1. Создаем опции для базы данных в памяти
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Уникальное имя для каждого теста
            .Options;

        // 2. Создаем настоящий контекст
        _context = new TestApplicationDbContext(options);

        // 3. Добавляем фейковые данные
        _context.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Существующий Цех" });
        _context.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Другой Цех" });
        _context.SaveChanges();

        _validator = new UpdateDepartmentCommandValidator(_context);
    }

    [TearDown]
    public void TearDown()
    {
        // Очищаем базу после каждого теста
        _context.Dispose();
    }

    [Test]
    public async Task Should_Have_Error_When_Name_Is_Duplicate()
    {
        // Arrange
        var command = new UpdateDepartmentCommand
        {
            Id = Guid.NewGuid(), // Новый ID (не совпадает с существующими)
            Name = "Существующий Цех" // Имя уже есть в базе
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Цех с таким названием уже существует.");
    }

    [Test]
    public async Task Should_Not_Have_Error_When_Name_Is_Unique()
    {
        // Arrange
        var command = new UpdateDepartmentCommand
        {
            Id = Guid.NewGuid(),
            Name = "Совершенно Новый Цех"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
