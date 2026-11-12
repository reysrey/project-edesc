using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Enums;
using KaizenBoard.Domain.Entities;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

public class CreateSuggestionCommandHandler : IRequestHandler<CreateSuggestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateSuggestionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }
    public async Task<Guid> Handle(CreateSuggestionCommand request, CancellationToken cancellationToken)
    {
        var entity = new Suggestion
        {
            Title = request.Title,
            CurrentFlaws = request.CurrentFlaws,
            ProposedSolution = request.ProposedSolution,
            ImprovementObject = request.ImprovementObject,
            TargetDepartmentId = request.TargetDepartmentId,
            Status = SuggestionStatus.Draft
        };

        _context.Suggestions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;

    }
}
