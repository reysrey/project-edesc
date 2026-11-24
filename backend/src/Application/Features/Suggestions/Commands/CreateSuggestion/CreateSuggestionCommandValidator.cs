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
    }
}
