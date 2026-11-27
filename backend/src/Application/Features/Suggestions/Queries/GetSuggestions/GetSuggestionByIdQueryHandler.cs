using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class GetSuggestionByIdQueryHandler : IRequestHandler<GetSuggestionByIdQuery, SuggestionDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public GetSuggestionByIdQueryHandler(
        IApplicationDbContext context,
        IUser currentUser,
        IIdentityService identityService)
    {
        _context = context;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<SuggestionDetailsDto> Handle(GetSuggestionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        var roles = _currentUser.Roles;
        var departmentId = _currentUser.DepartmentId;
        var suggestion = await _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(userId, roles, departmentId)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (suggestion is null)
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        var isMine = suggestion.CreatedBy == userId;
        var canModerate = SuggestionVisibility.CanModerate(roles, departmentId, suggestion.TargetDepartmentId);

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == suggestion.TargetDepartmentId, cancellationToken);

        var template = suggestion.TemplateId is { } templateId
            ? await _context.Templates.AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => new { t.Title, t.IsActive })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // authorName — ФИО автора, а не его идентификатор.
        // TODO(B06): для анонимной идеи обнулить и authorName, и createdBy.
        var authorName = suggestion.CreatedBy is null
            ? null
            : await _identityService.GetUserDisplayNameAsync(suggestion.CreatedBy);

        return new SuggestionDetailsDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            Status = suggestion.Status,
            Created = suggestion.Created,
            CreatedBy = suggestion.CreatedBy,
            AuthorName = authorName,
            IsMine = isMine,
            TargetDepartmentId = suggestion.TargetDepartmentId,
            DepartmentName = department?.Name,
            ImprovementObject = suggestion.ImprovementObject,
            CurrentFlaws = suggestion.CurrentFlaws,
            ProposedSolution = suggestion.ProposedSolution,
            TemplateId = suggestion.TemplateId,
            TemplateName = template?.Title,
            TemplateIsActive = template?.IsActive,
            RegistrationNumber = suggestion.RegistrationNumber,
            CommissionNote = isMine || canModerate ? suggestion.CommissionNote : null,
            AllowedActions = SuggestionVisibility.ComputeActions(suggestion.Status, isMine, canModerate),
            SubmissionCount = suggestion.SubmissionCount,
            // Маркер конкурентного изменения
            Version = SuggestionVersion.Compute(suggestion),
        };
    }
}
