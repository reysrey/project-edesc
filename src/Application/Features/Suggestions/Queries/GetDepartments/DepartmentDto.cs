namespace KaizenBoard.Application.Features.Departments.Queries.GetDepartments;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HeadUserId { get; set; }
    public int EmployeesCount { get; set; }
}
