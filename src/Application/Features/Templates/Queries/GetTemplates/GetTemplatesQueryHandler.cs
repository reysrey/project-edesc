using KaizenBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Templates.Queries.GetTemplates;

public class GetTemplatesQueryHandler : IRequestHandler<GetTemplatesQuery, List<TemplateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTemplatesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TemplateDto>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Templates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new TemplateDto
            {
                Id = t.Id,
                Type = t.Type,
                DepartmentId = t.DepartmentId,
                ImprovementObject = t.ImprovementObject,
                Title = t.Title,
                ProblemHint = t.ProblemHint,
                SolutionHint = t.SolutionHint,
                IsActive = t.IsActive
            })
            
            .ToListAsync(cancellationToken);
    }
}
