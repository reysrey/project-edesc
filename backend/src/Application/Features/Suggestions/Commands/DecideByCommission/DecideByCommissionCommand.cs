using FluentValidation;
using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Commands.DecideByCommission;

[Authorize(Roles = "AdminL1,AdminL2")]
public record DecideByCommissionCommand(Guid Id, string Decision, string Version, string? Note) : IRequest;

public class DecideByCommissionCommandValidator : AbstractValidator<DecideByCommissionCommand>
{
    public DecideByCommissionCommandValidator()
    {
        RuleFor(v => v.Decision).Must(value => value is "approve" or "return" or "reject");
        RuleFor(v => v.Version).NotEmpty().Length(64);
        RuleFor(v => v.Note).MaximumLength(10_000);
        RuleFor(v => v.Note).NotEmpty().WithMessage("Укажите причину решения.")
            .When(v => v.Decision is "return" or "reject");
    }
}

public class DecideByCommissionCommandHandler(IApplicationDbContext context, IUser user)
    : IRequestHandler<DecideByCommissionCommand>
{
    public async Task Handle(DecideByCommissionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await context.Suggestions.ExcludeForeignDrafts(user.Id)
            .SingleOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        if (!SuggestionVisibility.CanModerate(user.Roles, user.DepartmentId, suggestion.TargetDepartmentId))
            throw new ForbiddenAccessException();
        if (SuggestionVersion.Compute(suggestion) != request.Version)
            throw new InvalidOperationException("Запись уже изменена. Откройте актуальную карточку.");

        var previous = suggestion.Status;
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        switch (request.Decision)
        {
            case "approve": suggestion.ApproveByCommission(note); break;
            case "return": suggestion.ReturnByCommission(note!); break;
            case "reject": suggestion.RejectByCommission(note!); break;
            default: throw new ArgumentException("Неизвестное решение.");
        }

        var settings = await context.ReadServiceSettingsAsync(cancellationToken);
        suggestion.RecordActivity(previous, user.Id!, note, settings.NotificationsEnabled);
        await context.SaveChangesAsync(cancellationToken);
    }
}
