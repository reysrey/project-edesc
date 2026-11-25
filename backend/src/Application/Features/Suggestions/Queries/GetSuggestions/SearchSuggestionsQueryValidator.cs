using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class SearchSuggestionsQueryValidator : AbstractValidator<SearchSuggestionsQuery>
{
    public SearchSuggestionsQueryValidator()
    {
        RuleFor(v => v.Search).MaximumLength(200);
        RuleFor(v => v.Status).IsInEnum();
        RuleFor(v => v.To).Must((query, to) => !query.From.HasValue || !to.HasValue || query.From <= to)
            .WithMessage("Дата окончания не может предшествовать дате начала.");
    }
}
