using MediatR;

namespace KaizenBoard.Application.Features.Departments.Commands.UpdateDepartment;

public class UpdateDepartmentCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? HeadUserId { get; init; }
    public int EmployeesCount { get; init; }
}
