using KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;
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
}
