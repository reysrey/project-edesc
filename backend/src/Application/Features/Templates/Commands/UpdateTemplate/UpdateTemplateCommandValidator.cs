using FluentValidation;
using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Templates.Commands.UpdateTemplate;

public class UpdateTemplateCommandValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Название шаблона обязательно для заполнения.")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов.");

        // Для enum проверка на пустоту не работает, поэтому используем IsInEnum
        RuleFor(v => v.Type)
            .IsInEnum().WithMessage("Указан недопустимый тип шаблона.");

        RuleFor(v => v.ImprovementObject)
            .NotEmpty().WithMessage("Объект улучшения обязателен для заполнения.");
    }
}
