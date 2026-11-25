using System.Linq.Expressions;
using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands;

internal static class SuggestionInputRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator, Expression<Func<T, string>> title,
        Expression<Func<T, string>> problem, Expression<Func<T, string>> solution,
        Expression<Func<T, string>> improvementObject, Expression<Func<T, Guid>> department)
    {
        validator.RuleFor(title)
            .NotEmpty().WithMessage("Название обязательно.")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов.");
        validator.RuleFor(problem)
            .NotEmpty().WithMessage("Описание проблемы обязательно.")
            .MaximumLength(10_000).WithMessage("Описание проблемы не должно превышать 10 000 символов.");
        validator.RuleFor(solution)
            .NotEmpty().WithMessage("Описание решения обязательно.")
            .MaximumLength(10_000).WithMessage("Описание решения не должно превышать 10 000 символов.");
        validator.RuleFor(improvementObject)
            .NotEmpty().WithMessage("Объект улучшения обязателен.")
            .MaximumLength(200).WithMessage("Объект улучшения не должен превышать 200 символов.");
        validator.RuleFor(department)
            .NotEmpty().WithMessage("Подразделение обязательно.");
    }
}
