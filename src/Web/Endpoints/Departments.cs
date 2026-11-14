using KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;
using KaizenBoard.Application.Features.Departments.Commands.DeleteDepartment;
using KaizenBoard.Application.Features.Departments.Commands.UpdateDepartment;
using KaizenBoard.Application.Features.Departments.Queries.GetDepartments;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KaizenBoard.Web.Endpoints;

public class Departments : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateDepartmentCommand, "/").WithTags("Departments");
        groupBuilder.MapGet(GetDepartmentsQuery, "/").WithTags("Departments");
        groupBuilder.MapPut(UpdateDepartmentCommand, "/{id:guid}").WithTags("Departments");
        groupBuilder.MapDelete(DeleteDepartmentCommand, "/{id:guid}").WithTags("Departments");
    }

    public static async Task<Created<Guid>> CreateDepartmentCommand(ISender sender, CreateDepartmentCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/Departments/{id}", id);
    }

    public static async Task<Ok<List<DepartmentDto>>> GetDepartmentsQuery(ISender sender)
    {
        var list = await sender.Send(new GetDepartmentsQuery());
        return TypedResults.Ok(list);
    }

    public static async Task<Results<Ok<Guid>, BadRequest>> UpdateDepartmentCommand(ISender sender, Guid id, UpdateDepartmentCommand command)
    {
        if (id != command.Id)
            return TypedResults.BadRequest();

        var result = await sender.Send(command);
        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> DeleteDepartmentCommand(ISender sender, Guid id)
    {
        await sender.Send(new DeleteDepartmentCommand { Id = id });
        return TypedResults.NoContent();
    }
}
