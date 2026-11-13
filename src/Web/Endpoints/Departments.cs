using KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;
using KaizenBoard.Application.Features.Departments.Queries.GetDepartments;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KaizenBoard.Web.Endpoints;

public class Departments : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateDepartmentCommand, "/");
        groupBuilder.MapGet(GetDepartmentsQuery, "/");
    }

    public static async Task<Created<Guid>> CreateDepartmentCommand(ISender sender, CreateDepartmentCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/departments/{id}", id);
    }

    public static async Task<Ok<List<DepartmentDto>>> GetDepartmentsQuery(ISender sender)
    {
        var list = await sender.Send(new GetDepartmentsQuery());
        return TypedResults.Ok(list);
    }
}
