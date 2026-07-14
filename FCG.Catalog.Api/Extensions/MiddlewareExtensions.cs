using FCG.Catalog.Api.Middleware;

namespace FCG.Catalog.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
