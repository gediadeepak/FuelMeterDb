using System.Security.Claims;

namespace FuelMeter.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's database Id from the JWT sub claim.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirstValue("sub");

        return int.TryParse(value, out var id) ? id : 0;
    }
}
