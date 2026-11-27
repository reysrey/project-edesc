using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
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
        var suggestion = await _context.Suggestions
            .ExcludeForeignDrafts(_user.Id)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion == null) 
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        if (!SuggestionVisibility.CanModerate(_user.Roles, _user.DepartmentId, suggestion.TargetDepartmentId))
            throw new ForbiddenAccessException();

        var settings = await _context.ReadServiceSettingsAsync(cancellationToken);
        var previous = suggestion.Status;
        suggestion.ApproveByManager(request.RegNumber);
        suggestion.RecordActivity(previous, _user.Id!, null, settings.NotificationsEnabled);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
