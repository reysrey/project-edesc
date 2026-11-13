using MediatR;

namespace KaizenBoard.Application.Features.Departments.Queries.GetDepartments;

public class GetDepartmentsQuery : IRequest<List<DepartmentDto>>
{
}
