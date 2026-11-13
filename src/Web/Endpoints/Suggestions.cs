using KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;
using KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;
using KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;
using KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;
using KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KaizenBoard.Web.Endpoints;

public class Suggestions : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {

        groupBuilder.MapPost(CreateSuggestionCommand, "/");
        groupBuilder.MapGet(GetSuggestions, "/");
        groupBuilder.MapPost(SubmitSuggestion, "{id}/submit");
        groupBuilder.MapPost(ApproveByManager, "{id}/approve");
        groupBuilder.MapPost(ReturnByManager, "{id}/return");
    }

    public static async Task<Created<Guid>> CreateSuggestionCommand(ISender sender, CreateSuggestionCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/suggestions/{id}", id);
    }

    public static async Task<Ok<List<SuggestionDto>>> GetSuggestions(ISender sender)
    {
        var list = await sender.Send(new GetSuggestionsQuery());
        return TypedResults.Ok(list);
    }

    public static async Task<NoContent> SubmitSuggestion(ISender sender, Guid id)
    {
        await sender.Send(new SubmitSuggestionCommand {Id = id});
        return TypedResults.NoContent();
    }
    public static async Task<NoContent> ApproveByManager(ISender sender, Guid id, ApproveByManagerCommand command)
    {
        command.Id = id;
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> ReturnByManager(ISender sender, Guid id)
    {
        await sender.Send(new ReturnByManagerCommand { Id = id });
        return TypedResults.NoContent();
    }
}
