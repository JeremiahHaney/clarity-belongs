using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;

namespace ClarityBelongs.Web.Security;

public static class AntiforgeryEndpointExtensions
{
    public static RouteHandlerBuilder RequireAntiforgery(
        this RouteHandlerBuilder builder) =>
        builder.WithMetadata(new RequireAntiforgeryTokenAttribute(true));
}
