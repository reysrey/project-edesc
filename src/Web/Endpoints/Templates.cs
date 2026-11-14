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
        groupBuilder.MapPost(CreateTemplateCommand, "/").WithTags("Templates");
        groupBuilder.MapGet(GetTemplatesQuery, "/").WithTags("Templates");
        groupBuilder.MapPut(UpdateTemplateCommand, "/{id:guid}").WithTags("Templates");
        groupBuilder.MapPatch(ArchiveTemplateCommand, "/{id:guid}/archive").WithTags("Templates");
    }

    public static async Task<Created<Guid>> CreateTemplateCommand(ISender sender, CreateTemplateCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/Templates/{id}", id);
    }

    public static async Task<Ok<List<TemplateDto>>> GetTemplatesQuery(ISender sender)
    {
        var list = await sender.Send(new GetTemplatesQuery());
        return TypedResults.Ok(list);
    }

    public static async Task<Results<Ok<Guid>, BadRequest>> UpdateTemplateCommand(ISender sender, Guid id, UpdateTemplateCommand command)
    {
        if (id != command.Id)
            return TypedResults.BadRequest();

        var result = await sender.Send(command);
        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> ArchiveTemplateCommand(ISender sender, Guid id)
    {
        await sender.Send(new ArchiveTemplateCommand { Id = id });
        return TypedResults.NoContent();
    }
}
