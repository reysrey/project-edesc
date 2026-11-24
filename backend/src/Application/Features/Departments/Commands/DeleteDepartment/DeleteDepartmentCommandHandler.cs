using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Departments.Commands.DeleteDepartment;

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public DeleteDepartmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(Department), request.Id.ToString());
        }

        _context.Departments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
