using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var suggestion = await _context.Suggestions
            .ExcludeForeignDrafts(_user.Id)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (suggestion == null) 
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        if (!SuggestionVisibility.CanModerate(_user.Roles, _user.DepartmentId, suggestion.TargetDepartmentId))
            throw new ForbiddenAccessException();

        var settings = await _context.ReadServiceSettingsAsync(cancellationToken);
        var previous = suggestion.Status;
        suggestion.ReturnByManager(request.Reason);
        suggestion.RecordActivity(previous, _user.Id!, request.Reason, settings.NotificationsEnabled);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
