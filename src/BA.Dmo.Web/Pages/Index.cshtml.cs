using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Web.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages;

/// <summary>
/// Root route (Plan-V3 05_SHL section 5): "/" redirects to the landing.
/// The landing policy is fixed and global (UD-16/DS-01): Job On for every
/// authenticated user — never configurable per user/template, never a
/// role-specific redirect. Deterministic fallback to the first accessible
/// page only when Job On is genuinely unavailable; /no-access safe state
/// otherwise (GLM-SHL-06, no redirect loop).
/// </summary>
public class IndexModel : PageModel
{
    private readonly IdentityResolutionService _resolutionService;

    public IndexModel(IdentityResolutionService resolutionService)
    {
        _resolutionService = resolutionService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var rawClaim = User.FindFirst(SessionClaims.AuthUserIdClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(rawClaim) && Guid.TryParse(rawClaim, out var authUserId))
        {
            var resolution = await _resolutionService.ResolveAsync(authUserId, HttpContext.RequestAborted);
            if (resolution.IsSuccess && resolution.Value.FirstPage.Page is not null)
                return Redirect(resolution.Value.FirstPage.Page.Route);
        }

        // Session without a resolvable identity/access: safe state, no loop.
        return Redirect("/no-access");
    }
}
