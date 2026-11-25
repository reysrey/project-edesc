using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;

public class ReturnByManagerCommandValidator : AbstractValidator<ReturnByManagerCommand>
{
    public ReturnByManagerCommandValidator()
    {
        RuleFor(v => v.Reason)
            .NotEmpty().WithMessage("Укажите причину возврата.")
            .MaximumLength(10_000).WithMessage("Причина возврата не должна превышать 10 000 символов.");
    }
}
