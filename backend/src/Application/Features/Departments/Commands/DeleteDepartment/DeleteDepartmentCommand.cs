using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Constants;
using MediatR;

namespace KaizenBoard.Application.Features.Departments.Commands.DeleteDepartment;

[Authorize(Roles = Roles.AdminL1)]
public class DeleteDepartmentCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
}
