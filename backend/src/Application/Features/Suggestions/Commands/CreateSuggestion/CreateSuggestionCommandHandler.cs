using FluentValidation.Results;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;

using ValidationException = KaizenBoard.Application.Common.Exceptions.ValidationException;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

public class CreateSuggestionCommandHandler : IRequestHandler<CreateSuggestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateSuggestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateSuggestionCommand request, CancellationToken cancellationToken)
    {
        // B02: неизвестное подразделение — понятная ошибка поля, а не сбой БД.
        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == request.TargetDepartmentId, cancellationToken);

        if (!departmentExists)
        {
            throw new ValidationException(new[]
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
                throw new ValidationException(new[]
                {
                    new ValidationFailure("TemplateId", "Указанный шаблон не найден или недоступен.")
                });
            }
        }

        var entity = new Suggestion
        {
            Title = request.Title,
            CurrentFlaws = request.CurrentFlaws,
            ProposedSolution = request.ProposedSolution,
            ImprovementObject = request.ImprovementObject,
            TargetDepartmentId = request.TargetDepartmentId,
            TemplateId = request.TemplateId,
            Status = SuggestionStatus.Draft
        };

        _context.Suggestions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
