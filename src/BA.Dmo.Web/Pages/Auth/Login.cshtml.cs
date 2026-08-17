using System.Security.Claims;
using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Auth;

/// <summary>
/// Login page (Plan-V3 GLM-ACC-01, 05_SHL §5): Supabase Auth verifies the
/// credentials through the adapter; the session cookie then carries ONLY the
/// auth user id. Internal identity/grants are resolved server-side per
/// request. Error messages are generic (never reveal whether the email
/// exists — design contract). Post-login destination comes from the U-04
/// first-page resolution (global Job On landing; deterministic fallback) —
/// never a role-specific redirect.
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly ISupabaseAuthAdapter _authAdapter;
    private readonly IdentityResolutionService _resolutionService;

    public LoginModel(
        ISupabaseAuthAdapter authAdapter,
        IdentityResolutionService resolutionService)
    {
        _authAdapter = authAdapter;
        _resolutionService = resolutionService;
    }

    public string ErrorMessage { get; private set; } = string.Empty;

    public void OnGet()
    {
        // A session without access still lands here safely; no redirect loop.
    }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Credenciais inválidas.";
            return Page();
        }

        var signIn = await _authAdapter.SignInWithPasswordAsync(email, password, HttpContext.RequestAborted);
        if (signIn.IsFailure)
        {
            // Generic message for every failure (invalid credentials or
            // provider unavailable): no email-existence disclosure.
            ErrorMessage = "Credenciais inválidas.";
            return Page();
        }

        var identity = new ClaimsIdentity(
            [new Claim(SessionClaims.AuthUserIdClaimType, signIn.Value.AuthUserId.ToString())],
            SessionClaims.AuthenticationScheme);
        await HttpContext.SignInAsync(
            SessionClaims.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            });

        // Authoritative post-login destination: U-04 resolution (Job On
        // landing; canonical fallback when genuinely unavailable; /no-access
        // safe state otherwise).
        var resolution = await _resolutionService.ResolveAsync(
            signIn.Value.AuthUserId, HttpContext.RequestAborted);
        if (resolution.IsSuccess &&
            resolution.Value.FirstPage.Page is not null)
        {
            return Redirect(resolution.Value.FirstPage.Page.Route);
        }

        return Redirect("/no-access");
    }
}
