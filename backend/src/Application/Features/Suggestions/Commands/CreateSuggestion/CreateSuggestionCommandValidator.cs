using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

public class CreateSuggestionCommandValidator : AbstractValidator<CreateSuggestionCommand>
{
    public CreateSuggestionCommandValidator()
    {
        RuleFor(v => v.IdempotencyKey)
            .Matches(@"\A[A-Za-z0-9._~-]{1,128}\z")
            .WithMessage("Ключ идемпотентности должен содержать от 1 до 128 символов: латинские буквы, цифры, точка, подчёркивание, дефис или тильда.");
    }
}

public class CreateSuggestionInputValidator : AbstractValidator<CreateSuggestionInput>
{
    public CreateSuggestionInputValidator()
    {
        SuggestionInputRules.Apply(this, v => v.Title, v => v.CurrentFlaws,
            v => v.ProposedSolution, v => v.ImprovementObject, v => v.TargetDepartmentId);
    }
}
