using KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;
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
    }

    public static async Task<Created<Guid>> CreateSuggestionCommand(ISender sender, CreateSuggestionCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/suggestions/{id}", id);
    }
}
