using System.Text.Json.Serialization;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Web.Endpoints;

public class ServiceSettings : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();
        groupBuilder.MapGet(Get, "/");
        groupBuilder.MapPut(Update, "/").RequireAuthorization(policy => policy.RequireRole(Roles.AdminL1));
    }

    public static async Task<Ok<SettingsDto>> Get(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var settings = await context.ReadServiceSettingsAsync(cancellationToken);
        return TypedResults.Ok(SettingsDto.From(settings));
    }

    public static async Task<Ok<SettingsDto>> Update(
        IApplicationDbContext context, SettingsDto request, CancellationToken cancellationToken)
    {
        var stored = await context.ServiceSettings.SingleOrDefaultAsync(cancellationToken);
        if ((stored?.Version ?? Guid.Empty) != request.Version)
            throw new InvalidOperationException("Настройки уже изменились. Загрузите актуальные значения.");

        var settings = stored ?? new Domain.Entities.ServiceSettings();
        settings.SupportEnabled = request.SupportEnabled;
        settings.AnonymousEnabled = request.AnonymousEnabled;
        settings.NotificationsEnabled = request.NotificationsEnabled;
        settings.BoardEnabled = request.BoardEnabled;
        settings.Version = Guid.NewGuid();
        if (stored is null)
            context.ServiceSettings.Add(settings);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(SettingsDto.From(settings));
    }

    public record SettingsDto(
        [property: JsonRequired] bool SupportEnabled,
        [property: JsonRequired] bool AnonymousEnabled,
        [property: JsonRequired] bool NotificationsEnabled,
        [property: JsonRequired] bool BoardEnabled,
        [property: JsonRequired] Guid Version)
    {
        public static SettingsDto From(Domain.Entities.ServiceSettings settings) =>
            new(settings.SupportEnabled, settings.AnonymousEnabled, settings.NotificationsEnabled,
                settings.BoardEnabled, settings.Version);
    }
}
