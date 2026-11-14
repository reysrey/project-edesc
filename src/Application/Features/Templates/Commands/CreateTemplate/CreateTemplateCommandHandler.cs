using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;

public class CreateTemplateCommandHandler : IRequestHandler<CreateTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = new Template
        {
            Type = request.Type,
            DepartmentId = request.DepartmentId,
            ImprovementObject = request.ImprovementObject,
            Title = request.Title,
            ProblemHint = request.ProblemHint,
            SolutionHint = request.SolutionHint,
            IsActive = true
        };

        _context.Templates.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
