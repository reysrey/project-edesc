using System.Text.Json.Serialization;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Infrastructure.Data;
using KaizenBoard.Web.Infrastructure;
using KaizenBoard.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        foreach (var scheme in new[] { IdentityConstants.ApplicationScheme, IdentityConstants.TwoFactorUserIdScheme })
        {
            builder.Services.PostConfigure<CookieAuthenticationOptions>(scheme, options =>
                options.CookieManager = new TabSessionCookieManager(options.CookieManager ?? new ChunkingCookieManager()));
        }

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<IdentityApiOperationTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
        builder.Services.ConfigureHttpJsonOptions(options => 
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddCors();
    }
}
