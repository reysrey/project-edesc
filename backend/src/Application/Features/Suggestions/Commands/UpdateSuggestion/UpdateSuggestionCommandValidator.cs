using FluentValidation;

namespace KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;

public class UpdateSuggestionCommandValidator : AbstractValidator<UpdateSuggestionCommand>
{
    public UpdateSuggestionCommandValidator()
    {
        SuggestionInputRules.Apply(this, v => v.Title, v => v.CurrentFlaws,
            v => v.ProposedSolution, v => v.ImprovementObject, v => v.TargetDepartmentId);
        RuleFor(v => v.Version).NotEmpty()
            .WithMessage("Версия записи обязательна: получите её из GET /api/Suggestions/{id}.")
            .Length(64).WithMessage("Формат версии записи недопустим.");
    }
}
