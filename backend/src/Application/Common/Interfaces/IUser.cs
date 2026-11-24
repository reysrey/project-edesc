namespace KaizenBoard.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; }
    List<string>? Roles { get; }
    Guid? DepartmentId { get; }
}
