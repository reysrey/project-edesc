using KaizenBoard.Application.Features.Templates.Commands.ArchiveTemplate;
using KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;
using KaizenBoard.Application.Features.Templates.Commands.UpdateTemplate;
using KaizenBoard.Application.Features.Templates.Queries.GetTemplates;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KaizenBoard.Web.Endpoints;

public class Templates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreateTemplate, "/").WithTags("Templates");
        groupBuilder.MapGet(GetTemplates, "/").WithTags("Templates");
        groupBuilder.MapPut(UpdateTemplate, "/{id:guid}").WithTags("Templates");
        groupBuilder.MapPatch(ArchiveTemplate, "/{id:guid}/archive").WithTags("Templates");
    }

    public static async Task<Created<Guid>> CreateTemplate(ISender sender, CreateTemplateCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/Templates/{id}", id);
    }

    public static async Task<Ok<List<TemplateDto>>> GetTemplates(ISender sender)
    {
        var list = await sender.Send(new GetTemplatesQuery());
        return TypedResults.Ok(list);
    }

    public static async Task<Results<Ok<Guid>, BadRequest>> UpdateTemplate(ISender sender, Guid id, UpdateTemplateCommand command)
    {
        if (id != command.Id)
            return TypedResults.BadRequest();

        var result = await sender.Send(command);
        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> ArchiveTemplate(ISender sender, Guid id)
    {
        await sender.Send(new ArchiveTemplateCommand { Id = id });
        return TypedResults.NoContent();
    }
}
