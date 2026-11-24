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

namespace KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;

public class SubmitSuggestionCommandHandler : IRequestHandler<SubmitSuggestionCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public SubmitSuggestionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // B03: отправка заявки проверяет владельца, состояние и полноту данных
    public async Task<Unit> Handle(SubmitSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (suggestion is null)
        {
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        }

        // B03: отправлять может только автор заявки
        var isMine = suggestion.CreatedBy == _currentUser.Id;
        if (!isMine)
        {
            throw new ForbiddenAccessException();
        }

        // B03: отправка возможна только из Draft / ReturnedByManager
        if (suggestion.Status != SuggestionStatus.Draft &&
            suggestion.Status != SuggestionStatus.ReturnedByManager)
        {
            throw new InvalidOperationException("Отправить можно только черновик или предложение после доработки.");
        }

        // B03: обязательные поля должны быть заполнены
        if (string.IsNullOrWhiteSpace(suggestion.Title) ||
            string.IsNullOrWhiteSpace(suggestion.CurrentFlaws) ||
            string.IsNullOrWhiteSpace(suggestion.ProposedSolution) ||
            string.IsNullOrWhiteSpace(suggestion.ImprovementObject))
        {
            throw new ValidationException(new List<ValidationFailure>
            {
                new ValidationFailure("Title", "Заполните обязательные поля: заголовок, проблему, решение и объект улучшения.")
            });
        }

        suggestion.Submit();
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
