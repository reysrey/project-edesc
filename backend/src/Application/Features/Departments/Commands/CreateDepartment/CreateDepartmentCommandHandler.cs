using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateDepartmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        // Опционально: проверка на дубликат имени
        var exists = await _context.Departments.AnyAsync(d => d.Name == request.Name, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Подразделение с таким названием уже существует.");
        }

        var entity = new Department
        {
            Name = request.Name,
            HeadUserId = request.HeadUserId,
            EmployeesCount = request.EmployeesCount
        };

        _context.Departments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
