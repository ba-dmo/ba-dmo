using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.JobOn;

/// <summary>
/// Job On route surface (Plan-V3 05_SHL §5, UD-16): global landing of every
/// authenticated user, guarded by jobon.view (all active users). The module
/// content (folha/planeamento/histórico/definições, TD-20 modes) belongs to
/// its own roadmap unit (U-13); the shell only guarantees the route exists,
/// is server-side guarded and never exposes more than authorized.
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
