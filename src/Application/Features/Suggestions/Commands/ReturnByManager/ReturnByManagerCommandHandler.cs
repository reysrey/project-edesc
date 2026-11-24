using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;

public class ReturnByManagerCommandHandler : IRequestHandler<ReturnByManagerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public ReturnByManagerCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // B03: return проверяет права модератора и область подраздела
    public async Task<Unit> Handle(ReturnByManagerCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion is null) throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        var roles = _currentUser.Roles ?? new List<string>();
        if (!roles.Contains("AdminL2") && !roles.Contains("AdminL1"))
        {
            throw new ForbiddenAccessException();
        }

        if (roles.Contains("AdminL2"))
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.HeadUserId == _currentUser.Id && d.Id == suggestion.TargetDepartmentId, cancellationToken);
            if (department is null) throw new ForbiddenAccessException();
        }

        suggestion.ReturnByManager(request.Reason);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
