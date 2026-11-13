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
            .NotEmpty().WithMessage("Type is required.");

        RuleFor(v => v.ImprovementObject)
            .NotEmpty().WithMessage("ImprovementObject is required.");
    }
}
