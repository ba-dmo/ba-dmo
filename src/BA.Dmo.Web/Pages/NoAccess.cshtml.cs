using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages;

/// <summary>
/// Safe state (Plan-V3 GLM-SHL-06, GLM-ACC-01.6): valid session whose
/// internal user/template grants nothing — message, logout available, no
/// data. Never silently elevated to any module (GLM-ARCH-18).
/// </summary>
[AllowAnonymous]
public class NoAccessModel : PageModel
{
    public void OnGet()
    {
    }
}
