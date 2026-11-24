using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace KaizenBoard.Web.Infrastructure;

/// <summary>
/// Adds the Bearer security scheme to the OpenAPI document when Bearer authentication
/// is configured, enabling the Scalar UI to send <c>Authorization: Bearer &lt;token&gt;</c>
/// headers from the interactive documentation.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (!authenticationSchemes.Any(authScheme => authScheme.Name == IdentityConstants.BearerScheme))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "Identity bearer token"
        };
    }
}
