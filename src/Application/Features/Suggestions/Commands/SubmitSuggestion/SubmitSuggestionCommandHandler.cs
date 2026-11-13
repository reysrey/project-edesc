using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;
using KaizenBoard.Domain.Entities;

namespace KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;

public class SubmitSuggestionCommandHandler: IRequestHandler<SubmitSuggestionCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    public SubmitSuggestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(SubmitSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (suggestion == null) throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        suggestion.Submit();
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
