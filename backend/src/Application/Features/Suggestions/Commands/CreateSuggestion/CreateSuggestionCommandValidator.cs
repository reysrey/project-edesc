using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

internal class CreateSuggestionCommandValidator: AbstractValidator<CreateSuggestionCommand>
{
    public CreateSuggestionCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Название обязательно.")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов.");

        RuleFor(v => v.CurrentFlaws)
            .NotEmpty().WithMessage("Описание проблемы обязательно.");

        RuleFor(v => v.ProposedSolution)
            .NotEmpty().WithMessage("Описание решения обязательно.");

        // B02: объект улучшения и подразделение обязательны,
        // чтобы заявка не создавалась «пустой» для модерации.
        RuleFor(v => v.ImprovementObject)
            .NotEmpty().WithMessage("Объект улучшения обязателен.")
            .MaximumLength(200).WithMessage("Объект улучшения не должен превышать 200 символов.");

        RuleFor(v => v.TargetDepartmentId)
            .NotEmpty().WithMessage("Подразделение обязательно.");
    }
}
