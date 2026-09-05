using Microsoft.AspNetCore.Antiforgery;

namespace ClarityBelongs.Web.Services;

public static class AntiforgeryEndpointExtensions
{
    public static RouteHandlerBuilder RequireValidatedAntiforgery(
        this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(
            async (context, next) =>
            {
                var antiforgery = context.HttpContext.RequestServices
                    .GetRequiredService<IAntiforgery>();

                try
                {
                    await antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.BadRequest();
                }

                return await next(context);
            });
    }
}
