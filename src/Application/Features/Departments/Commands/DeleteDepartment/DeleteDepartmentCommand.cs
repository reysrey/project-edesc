using MediatR;

namespace KaizenBoard.Application.Features.Departments.Commands.DeleteDepartment;

public class DeleteDepartmentCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
}
