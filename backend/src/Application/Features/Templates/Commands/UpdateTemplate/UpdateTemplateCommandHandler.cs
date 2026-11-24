using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Templates.Commands.UpdateTemplate;

public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public UpdateTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(Template), request.Id.ToString());
        }

        entity.Type = request.Type;
        entity.DepartmentId = request.DepartmentId;
        entity.ImprovementObject = request.ImprovementObject;
        entity.Title = request.Title;
        entity.ProblemHint = request.ProblemHint;
        entity.SolutionHint = request.SolutionHint;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
