using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;

public class ApproveByManagerCommandHandler : IRequestHandler<ApproveByManagerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    
    public ApproveByManagerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Unit> Handle(ApproveByManagerCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion == null) throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        suggestion.ApproveByManager(request.RegNumber);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }


}
