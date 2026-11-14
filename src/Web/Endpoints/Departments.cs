using KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;
using KaizenBoard.Application.Features.Departments.Queries.GetDepartments;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KaizenBoard.Web.Endpoints;

public class Departments : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateDepartment, "/");
        groupBuilder.MapGet(GetDepartments, "/");
    }

    public static async Task<Created<Guid>> CreateDepartment(ISender sender, CreateDepartmentCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/departments/{id}", id);
    }

    public static async Task<Ok<List<DepartmentDto>>> GetDepartments(ISender sender)
    {
        var list = await sender.Send(new GetDepartmentsQuery());
        return TypedResults.Ok(list);
    }
}
