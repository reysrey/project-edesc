using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation.Results;
using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = KaizenBoard.Application.Common.Exceptions.ValidationException;

namespace KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;

public class UpdateSuggestionCommandHandler : IRequestHandler<UpdateSuggestionCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateSuggestionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateSuggestionCommand request, CancellationToken cancellationToken)
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
                new ValidationFailure("TargetDepartmentId", "Неизвестный подразделение.")
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.CurrentFlaws) ||
            string.IsNullOrWhiteSpace(request.ProposedSolution) ||
            string.IsNullOrWhiteSpace(request.ImprovementObject))
        {
            throw new ValidationException(new List<ValidationFailure>
            {
                new ValidationFailure("Title", "Заполните обязательные поля: заголовок, проблему, решение и объект улучшения.")
            });
        }

        suggestion.Title = request.Title;
        suggestion.CurrentFlaws = request.CurrentFlaws;
        suggestion.ProposedSolution = request.ProposedSolution;
        suggestion.ImprovementObject = request.ImprovementObject;
        suggestion.TargetDepartmentId = request.TargetDepartmentId;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
