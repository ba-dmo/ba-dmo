using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Historia;

/// <summary>
/// História route surface (Plan-V3 05_SHL section 5): guarded server-side by
/// module presence in the resolved template grants (GLM-ACC-02/04). The
/// transversal read content — limited to the user's authorized modules
/// (TD-24) — belongs to its own roadmap unit (U-18).
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
