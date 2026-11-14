using KaizenBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Departments.Commands.UpdateDepartment;

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public UpdateDepartmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Цех с идентификатором {request.Id} не найден.");
        }

        entity.Name = request.Name;
        entity.HeadUserId = request.HeadUserId;
        entity.EmployeesCount = request.EmployeesCount;

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
