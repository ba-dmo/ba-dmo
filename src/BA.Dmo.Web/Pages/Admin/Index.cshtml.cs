using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Admin;

/// <summary>
/// Administração — entry page (Plan-V3 04_ACC §9, GLM-ACC-06). Page access
/// is enforced by the admin.gerir policy; every mutation is additionally
/// re-authorized server-side inside the Application services. The "Voltar ao
/// Job On" link reflects that Job On — not Administração — is the global
/// landing (UD-16).
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
