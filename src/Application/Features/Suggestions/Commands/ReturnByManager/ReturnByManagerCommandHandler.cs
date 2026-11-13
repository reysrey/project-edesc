using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;

public class ReturnByManagerCommandHandler : IRequestHandler<ReturnByManagerCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public ReturnByManagerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Unit> Handle(ReturnByManagerCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion == null) throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        suggestion.ReturnByManager(request.Reason);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
