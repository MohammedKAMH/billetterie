using System.Security.Claims;

namespace Billetterie.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetKeycloakId(this ClaimsPrincipal user)
            => user.FindFirst("sub")?.Value;

        public static string? GetEmail(this ClaimsPrincipal user)
            => user.FindFirst("email")?.Value;
    }
}