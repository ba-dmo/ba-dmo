using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.ReparacaoExterna;

/// <summary>
/// Reparação Externa route surface (Plan-V3 05_SHL section 5): guarded
/// server-side by module presence in the resolved template grants
/// (GLM-ACC-02/04). The module content belongs to its own roadmap unit (U-15).
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
