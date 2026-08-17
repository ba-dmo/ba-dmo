using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages;

/// <summary>
/// Safe state (Plan-V3 GLM-SHL-05/06, GLM-ACC-07 scenario 9): authenticated
/// session without access — no data, no redirect loop, logout available.
/// </summary>
[AllowAnonymous]
public class AccessDeniedModel : PageModel
{
    public void OnGet()
    {
    }
}
