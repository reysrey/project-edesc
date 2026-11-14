using KaizenBoard.Application.Common.Security;
using MediatR;

namespace KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;

[Authorize(Roles = "AdminL1,AdminL2")]
public class CreateDepartmentCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? HeadUserId { get; init; } // Сделали nullable string, как в сущности
    public int EmployeesCount { get; init; }
}
