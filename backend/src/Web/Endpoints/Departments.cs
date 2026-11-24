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
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreateDepartment, "/").WithTags("Departments");
        groupBuilder.MapGet(GetDepartments, "/").WithTags("Departments");
        groupBuilder.MapPut(UpdateDepartment, "/{id:guid}").WithTags("Departments");
        groupBuilder.MapDelete(DeleteDepartment, "/{id:guid}").WithTags("Departments");
    }

    public static async Task<Created<Guid>> CreateDepartment(ISender sender, CreateDepartmentCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/Departments/{id}", id);
    }

    public static async Task<Ok<List<DepartmentDto>>> GetDepartments(ISender sender)
    {
        var list = await sender.Send(new GetDepartmentsQuery());
        return TypedResults.Ok(list);
    }

    public static async Task<Results<Ok<Guid>, BadRequest>> UpdateDepartment(ISender sender, Guid id, UpdateDepartmentCommand command)
    {
        if (id != command.Id)
            return TypedResults.BadRequest();

        var result = await sender.Send(command);
        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> DeleteDepartment(ISender sender, Guid id)
    {
        await sender.Send(new DeleteDepartmentCommand { Id = id });
        return TypedResults.NoContent();
    }
}
