using KaizenBoard.Application.Common.Models;
using KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;
using KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;
using KaizenBoard.Application.Features.Suggestions.Commands.DecideByCommission;
using KaizenBoard.Application.Features.Suggestions.Commands.SupportSuggestion;
using KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;
using KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;
using KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;
using KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;
using KaizenBoard.Domain.Enums;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KaizenBoard.Web.Endpoints;

public class Suggestions : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();
        groupBuilder.MapPost(CreateSuggestion, "/")
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        groupBuilder.MapGet(GetSuggestions, "/");

        // ВАЖНО: search регистрируется ДО {id}
        groupBuilder.MapGet(SearchSuggestions, "search");
        groupBuilder.MapGet(GetSuggestionById, "{id}")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        groupBuilder.MapPut(UpdateSuggestion, "{id}")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        groupBuilder.MapPost(SubmitSuggestion, "{id}/submit")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        groupBuilder.MapPost(ApproveByManager, "{id}/approve");
        groupBuilder.MapPost(ReturnByManager, "{id}/return");
        groupBuilder.MapPost(DecideByCommission, "{id}/commission/{decision}");
        groupBuilder.MapPut(SetSupport, "{id}/support");
        groupBuilder.MapDelete(RemoveSupport, "{id}/support");
    }

    public static async Task<Created<Guid>> CreateSuggestion(
        ISender sender,
        CreateSuggestionInput input,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var id = await sender.Send(new CreateSuggestionCommand(input, idempotencyKey));
        return TypedResults.Created($"/api/suggestions/{id}", id);
    }

    public static async Task<Ok<List<SuggestionDto>>> GetSuggestions(ISender sender)
    {
        var list = await sender.Send(new GetSuggestionsQuery());
        return TypedResults.Ok(list);
    }

    // --- НОВЫЙ МЕТОД ---
    public static async Task<Ok<PagedResult<SuggestionDto>>> SearchSuggestions(
        ISender sender,
        [FromQuery] string? scope,
        [FromQuery] string? search,
        [FromQuery] SuggestionStatus? status,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sort,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await sender.Send(new SearchSuggestionsQuery
        {
            Scope = scope,
            Search = search,
            Status = status,
            DepartmentId = departmentId,
            From = from,
            To = to,
            Sort = sort,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
        return TypedResults.Ok(result);
    }
    // -------------------

    public static async Task<Ok<SuggestionDetailsDto>> GetSuggestionById(ISender sender, Guid id)
    {
        var dto = await sender.Send(new GetSuggestionByIdQuery(id));
        return TypedResults.Ok(dto);
    }

    public static async Task<Ok<UpdateSuggestionResponse>> UpdateSuggestion(
        ISender sender, Guid id, UpdateSuggestionCommand command)
    {
        command.Id = id;
        var version = await sender.Send(command);
        return TypedResults.Ok(new UpdateSuggestionResponse(version));
    }

    public static async Task<NoContent> SubmitSuggestion(ISender sender, Guid id)
    {
        await sender.Send(new SubmitSuggestionCommand { Id = id });
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> ApproveByManager(ISender sender, Guid id, ApproveByManagerCommand command)
    {
        command.Id = id;
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> ReturnByManager(ISender sender, Guid id, ReturnByManagerCommand command)
    {
        command.Id = id;
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> DecideByCommission(
        ISender sender, Guid id, string decision, CommissionDecision body)
    {
        await sender.Send(new DecideByCommissionCommand(id, decision, body.Version, body.Note));
        return TypedResults.NoContent();
    }

    public static Task<NoContent> SetSupport(ISender sender, Guid id) => ChangeSupport(sender, id, true);

    public static Task<NoContent> RemoveSupport(ISender sender, Guid id) => ChangeSupport(sender, id, false);

    private static async Task<NoContent> ChangeSupport(ISender sender, Guid id, bool supported)
    {
        await sender.Send(new SetSuggestionSupportCommand(id, supported));
        return TypedResults.NoContent();
    }

    public record CommissionDecision(string Version, string? Note);
}

public record UpdateSuggestionResponse(string Version);
