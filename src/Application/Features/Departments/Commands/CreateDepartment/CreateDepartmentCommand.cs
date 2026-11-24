using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Constants;
using MediatR;

namespace KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;

[Authorize(Roles = Roles.AdminL1)]
public class CreateDepartmentCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? HeadUserId { get; init; }
    public int EmployeesCount { get; init; }
}
