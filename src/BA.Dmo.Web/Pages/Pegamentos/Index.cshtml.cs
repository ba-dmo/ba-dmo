using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Pegamentos;

/// <summary>
/// Pegamentos route surface (Plan-V3 05_SHL section 5): guarded server-side
/// by module presence in the resolved template grants (GLM-ACC-02/04).
/// Separate from Peso — shared navigation area only, never fused logic
/// (UD-05/GLM-CTR-03). The module content belongs to its own roadmap
/// unit (U-11).
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
