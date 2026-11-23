using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class GetSuggestionByIdQueryHandler : IRequestHandler<GetSuggestionByIdQuery, SuggestionDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetSuggestionByIdQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SuggestionDetailsDto> Handle(GetSuggestionByIdQuery request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.Suggestions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (suggestion is null)
        {
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        }

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == suggestion.TargetDepartmentId, cancellationToken);

        var isMine = suggestion.CreatedBy == _currentUser.Id;

        var allowedActions = new List<string>();
        if (isMine)
        {
            allowedActions.Add("edit");
            if (suggestion.Status == SuggestionStatus.Draft ||
                suggestion.Status == SuggestionStatus.ReturnedByManager ||
                suggestion.Status == SuggestionStatus.RevisionRequired)
            {
                allowedActions.Add("submit");
            }
        }

        if (suggestion.Status == SuggestionStatus.Submitted)
        {
            allowedActions.Add("moderate");
        }

        if (suggestion.Status == SuggestionStatus.InCommission)
        {
            allowedActions.Add("commissionApprove");
            allowedActions.Add("commissionReturn");
            allowedActions.Add("commissionReject");
        }

        if (suggestion.Status == SuggestionStatus.ApprovedPlanning)
        {
            allowedActions.Add("implementationStart");
        }

        if (suggestion.Status == SuggestionStatus.InImplementation)
        {
            allowedActions.Add("implementationComplete");
        }

        return new SuggestionDetailsDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            Status = suggestion.Status,
            Created = suggestion.Created,
            CreatedBy = suggestion.CreatedBy,
            AuthorName = suggestion.CreatedBy,
            IsMine = isMine,
            TargetDepartmentId = suggestion.TargetDepartmentId,
            DepartmentName = department?.Name,
            ImprovementObject = suggestion.ImprovementObject,
            CurrentFlaws = suggestion.CurrentFlaws,
            ProposedSolution = suggestion.ProposedSolution,
            TemplateId = suggestion.TemplateId,
            RegistrationNumber = suggestion.RegistrationNumber,
            CommissionNote = suggestion.CommissionNote,
            AllowedActions = allowedActions,
        };
    }
}
