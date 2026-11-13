using KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;
using KaizenBoard.Application.Features.Templates.Queries.GetTemplates;
using Microsoft.AspNetCore.Http.HttpResults;
using MediatR; // Убедись, что ISender доступен!

namespace KaizenBoard.Web.Endpoints; // Тот же namespace, что и у Departments!

public class Templates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateTemplateCommand, "/");
        groupBuilder.MapGet(GetTemplatesQuery, "/");
    }

    public static async Task<Created<Guid>> CreateTemplateCommand(ISender sender, CreateTemplateCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/templates/{id}", id);
    }

    public static async Task<Ok<List<TemplateDto>>> GetTemplatesQuery(ISender sender)
    {
        var list = await sender.Send(new GetTemplatesQuery());
        return TypedResults.Ok(list);
    }
}
