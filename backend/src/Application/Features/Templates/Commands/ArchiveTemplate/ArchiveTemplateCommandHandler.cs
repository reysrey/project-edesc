using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Templates.Commands.ArchiveTemplate;

public class ArchiveTemplateCommandHandler : IRequestHandler<ArchiveTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public ArchiveTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(ArchiveTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(Template), request.Id.ToString());
        }

        entity.IsActive = false; // Мягкое удаление (архивация)
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
