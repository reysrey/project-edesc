using FluentValidation.Results;
using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;

using ValidationException = KaizenBoard.Application.Common.Exceptions.ValidationException;

namespace KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;

public class UpdateSuggestionCommandHandler : IRequestHandler<UpdateSuggestionCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateSuggestionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(UpdateSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (suggestion is null)
        {
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        }

        var isMine = suggestion.CreatedBy == _currentUser.Id;
        if (!isMine)
        {
            throw new ForbiddenAccessException();
        }

        if (!string.Equals(SuggestionVersion.Compute(suggestion), request.Version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Запись уже изменена. Откройте её заново и повторите правку.");
        }

        if (suggestion.Status != SuggestionStatus.Draft &&
            suggestion.Status != SuggestionStatus.ReturnedByManager &&
            suggestion.Status != SuggestionStatus.RevisionRequired)
        {
            throw new InvalidOperationException("Редактировать можно только черновик или предложение после возврата на доработку.");
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.TargetDepartmentId, cancellationToken);

        if (department is null)
        {
            throw new ValidationException(new List<ValidationFailure>
            {
                new ValidationFailure("TargetDepartmentId", "Указанное подразделение не найдено.")
            });
        }

        // B02: привязанный шаблон должен существовать и быть активным.
        if (request.TemplateId is { } templateId)
        {
            var templateAvailable = await _context.Templates
                .AnyAsync(t => t.Id == templateId && t.IsActive, cancellationToken);

            if (!templateAvailable)
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure("TemplateId", "Указанный шаблон не найден или недоступен.")
                });
            }
        }

        suggestion.Title = request.Title;
        suggestion.CurrentFlaws = request.CurrentFlaws;
        suggestion.ProposedSolution = request.ProposedSolution;
        suggestion.ImprovementObject = request.ImprovementObject;
        suggestion.TargetDepartmentId = request.TargetDepartmentId;
        suggestion.TemplateId = request.TemplateId;

        // B02: обычное сохранение не переводит статус в Submitted — это делает /submit.

        await _context.SaveChangesAsync(cancellationToken);

        // B02: возвращаем актуальную версию уже после фиксации LastModified.
        return SuggestionVersion.Compute(suggestion);
    }
}
