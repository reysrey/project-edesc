using Microsoft.AspNetCore.Authentication.Cookies;

namespace KaizenBoard.Web.Infrastructure;

internal sealed class TabSessionCookieManager(ICookieManager cookies) : ICookieManager
{
    private const string TabHeader = "X-IdeaHub-Tab";

    public string? GetRequestCookie(HttpContext context, string key) =>
        CookieName(context, key) is { } name ? cookies.GetRequestCookie(context, name) : null;

    public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
    {
        var name = CookieName(context, key) ?? throw new BadHttpRequestException($"Invalid {TabHeader} header.");
        cookies.AppendResponseCookie(context, name, value, options);
    }

    public void DeleteCookie(HttpContext context, string key, CookieOptions options)
    {
        if (CookieName(context, key) is { } name)
            cookies.DeleteCookie(context, name, options);
    }

    private static string? CookieName(HttpContext context, string key)
    {
        if (!context.Request.Headers.TryGetValue(TabHeader, out var values))
            return key;

        return values.Count == 1 && Guid.TryParseExact(values[0], "D", out var tabId)
            ? $"{key}.{tabId:N}"
            : null;
    }
}
