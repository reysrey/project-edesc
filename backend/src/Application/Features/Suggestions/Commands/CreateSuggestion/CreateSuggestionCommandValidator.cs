using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

public class CreateSuggestionCommandValidator : AbstractValidator<CreateSuggestionCommand>
{
    public CreateSuggestionCommandValidator()
    {
        SuggestionInputRules.Apply(this, v => v.Title, v => v.CurrentFlaws,
            v => v.ProposedSolution, v => v.ImprovementObject, v => v.TargetDepartmentId);
    }
}
