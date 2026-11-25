namespace KaizenBoard.Web.Infrastructure;

internal static class BrowserOriginProtection
{
    public static bool Allows(HttpRequest request, string[] allowedOrigins)
    {
        var method = request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
            return true;

        var origins = request.Headers.Origin;
        if (origins.Count == 0)
        {
            // Native API clients do not send browser fetch metadata.
            var site = request.Headers["Sec-Fetch-Site"].ToString();
            return site is "" or "same-origin" or "none";
        }

        if (origins.Count != 1 || origins[0] is not { Length: > 0 } origin || origin == "null")
            return false;

        var requestOrigin = $"{request.Scheme}://{request.Host}";
        return string.Equals(origin, requestOrigin, StringComparison.OrdinalIgnoreCase)
            || allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
