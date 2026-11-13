using MediatR;

namespace KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;

public class CreateDepartmentCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? HeadUserId { get; init; } // Сделали nullable string, как в сущности
    public int EmployeesCount { get; init; }
}
