using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;
using KaizenBoard.Domain.Constants;
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
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());

        var isMine = suggestion.CreatedBy == _currentUser.Id;
        var isManager = _currentUser.Roles?.Contains(Roles.AdminL1) == true;
        var isModerator = _currentUser.Roles?.Contains(Roles.AdminL2) == true;

        // Защита от чтения чужих черновиков по ID (B03.1)
        if (suggestion.Status == SuggestionStatus.Draft && !isMine)
        {
            // Модератор своего подразделения тоже может видеть черновики (если нужно), 
            // но по правилам IdeaHub обычно нет. Оставляем только автора.
            throw new NotFoundException(nameof(Suggestion), request.Id.ToString());
        }

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == suggestion.TargetDepartmentId, cancellationToken);

        var allowedActions = SuggestionVisibility.ComputeActions(
            suggestion, _currentUser.Id, isManager, isModerator);

        return new SuggestionDetailsDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            Status = suggestion.Status,
            Created = suggestion.Created,
            CreatedBy = suggestion.CreatedBy,
            AuthorName = suggestion.CreatedBy, // TODO(B06): ФИО автора
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
