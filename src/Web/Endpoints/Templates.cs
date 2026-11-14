using KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;
using KaizenBoard.Application.Features.Templates.Queries.GetTemplates;
using Microsoft.AspNetCore.Http.HttpResults;
using MediatR;

namespace KaizenBoard.Web.Endpoints;

public class Templates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();
        groupBuilder.MapPost(CreateTemplate, "/");
        groupBuilder.MapGet(GetTemplates, "/");
    }

    public static async Task<Created<Guid>> CreateTemplate(ISender sender, CreateTemplateCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/templates/{id}", id);
    }

    public static async Task<Ok<List<TemplateDto>>> GetTemplates(ISender sender)
    {
        var list = await sender.Send(new GetTemplatesQuery());
        return TypedResults.Ok(list);
    }
}
