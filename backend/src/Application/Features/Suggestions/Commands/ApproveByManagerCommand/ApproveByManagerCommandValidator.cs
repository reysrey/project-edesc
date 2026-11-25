using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;

public class ApproveByManagerCommandValidator : AbstractValidator<ApproveByManagerCommand>
{
    public ApproveByManagerCommandValidator()
    {
        RuleFor(v => v.RegNumber)
            .NotEmpty().WithMessage("Регистрационный номер обязателен.")
            .MaximumLength(200).WithMessage("Регистрационный номер не должен превышать 200 символов.");
    }
}
