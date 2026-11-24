using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;

public class ApproveByManagerCommandHandler : IRequestHandler<ApproveByManagerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ApproveByManagerCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Unit> Handle(ApproveByManagerCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion == null) 
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        // Модератор (AdminL2) может одобрять только идеи своего подразделения.
        // Руководитель (AdminL1) может всё.
        var isManager = _user.Roles?.Contains(Roles.AdminL1) == true;
        if (!isManager && suggestion.TargetDepartmentId != _user.DepartmentId)
            throw new ForbiddenAccessException();

        suggestion.ApproveByManager(request.RegNumber);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}