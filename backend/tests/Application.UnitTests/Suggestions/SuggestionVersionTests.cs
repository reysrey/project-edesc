using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace KaizenBoard.Application.UnitTests.Suggestions;

[TestFixture]
public class SuggestionVersionTests
{
    private static Suggestion CreateSuggestion(string title = "Переписать БД на PostgreSQL")
    {
        return new Suggestion
        {
            Id = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            Title = title,
            Status = SuggestionStatus.Draft,
        };
    }

    [Test]
    public void Compute_ReturnsSameHash_ForIdenticalSuggestion()
    {
        var suggestion = CreateSuggestion();

        var hash1 = SuggestionVersion.Compute(suggestion);
        var hash2 = SuggestionVersion.Compute(suggestion);

        hash1.ShouldBe(hash2);
    }

    [Test]
    public void Compute_ReturnsDifferentHash_WhenTitleChanges()
    {
        var suggestion = CreateSuggestion("Переписать БД на PostgreSQL");
        var hash1 = SuggestionVersion.Compute(suggestion);

        suggestion.Title = "Переписать БД на ClickHouse";
        var hash2 = SuggestionVersion.Compute(suggestion);

        hash2.ShouldNotBe(hash1);
    }

    [Test]
    public void Compute_ReturnsDifferentHash_WhenStatusChanges()
    {
        var suggestion = CreateSuggestion();
        var hash1 = SuggestionVersion.Compute(suggestion);

        suggestion.Status = SuggestionStatus.Submitted;
        var hash2 = SuggestionVersion.Compute(suggestion);

        hash2.ShouldNotBe(hash1);
    }

    [Test]
    public void Compute_ReturnsHexString_OfFixedLength()
    {
        var suggestion = CreateSuggestion();

        var hash = SuggestionVersion.Compute(suggestion);

        hash.Length.ShouldBe(64);
        hash.ShouldMatch("^[0-9A-F]+$");
    }
}
