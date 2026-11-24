using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
using MediatR;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;

public class ReturnByManagerCommandHandler : IRequestHandler<ReturnByManagerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ReturnByManagerCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Unit> Handle(ReturnByManagerCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion == null)
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        var isManager = _user.Roles?.Contains(Roles.AdminL1) == true;
        if (!isManager && suggestion.TargetDepartmentId != _user.DepartmentId)
            throw new ForbiddenAccessException();

        suggestion.ReturnByManager(request.Reason);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
