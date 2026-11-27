using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;

public class SubmitSuggestionCommandHandler : IRequestHandler<SubmitSuggestionCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SubmitSuggestionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Unit> Handle(SubmitSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions
            .ExcludeForeignDrafts(_user.Id)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (suggestion == null) 
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        // Проверка владельца (B03.2)
        if (suggestion.CreatedBy != _user.Id)
            throw new ForbiddenAccessException();

        var previous = suggestion.Status;
        suggestion.Submit();
        suggestion.RecordActivity(previous, _user.Id!, null, false);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
