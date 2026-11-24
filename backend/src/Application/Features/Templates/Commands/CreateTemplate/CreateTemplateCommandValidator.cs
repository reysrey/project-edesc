using FluentValidation;

namespace KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;

public class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(v => v.Type)
            .IsInEnum().WithMessage("Недопустимый тип шаблона");

        RuleFor(v => v.ImprovementObject)
            .NotEmpty().WithMessage("ImprovementObject is required.");
    }
}
