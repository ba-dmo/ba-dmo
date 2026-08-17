using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Boquilhas;

/// <summary>
/// Boquilhas route surface (Plan-V3 05_SHL section 5): guarded server-side by
/// module presence in the resolved template grants (GLM-ACC-02/04). The
/// module content belongs to its own roadmap unit (U-19).
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
