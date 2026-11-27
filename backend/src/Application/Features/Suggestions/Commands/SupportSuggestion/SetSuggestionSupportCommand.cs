using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Commands.SupportSuggestion;

[Authorize]
public record SetSuggestionSupportCommand(Guid Id, bool Supported) : IRequest;

public class SetSuggestionSupportCommandHandler(IApplicationDbContext context, IUser user)
    : IRequestHandler<SetSuggestionSupportCommand>
{
    public async Task Handle(SetSuggestionSupportCommand request, CancellationToken cancellationToken)
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var settings = await context.ReadServiceSettingsAsync(cancellationToken);
        var suggestion = await context.Suggestions.AsNoTracking()
            .ApplyVisibility(userId, user.Roles, user.DepartmentId, settings.BoardEnabled)
            .Where(s => s.Id == request.Id)
            .Select(s => new { s.Status, s.CreatedBy })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        if (!SuggestionVisibility.CanSupport(suggestion.Status, suggestion.CreatedBy == userId, settings.SupportEnabled))
            throw new ForbiddenAccessException();

        var existing = context.SuggestionSupports.Where(s => s.SuggestionId == request.Id && s.UserId == userId);
        if (!request.Supported)
        {
            await existing.ExecuteDeleteAsync(cancellationToken);
            return;
        }

        if (await existing.AnyAsync(cancellationToken))
            return;

        var support = new SuggestionSupport { SuggestionId = request.Id, UserId = userId };
        context.SuggestionSupports.Add(support);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await existing.AnyAsync(cancellationToken))
                throw;
            context.SuggestionSupports.Entry(support).State = EntityState.Detached;
        }
    }
}
