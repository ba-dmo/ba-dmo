using BA.Dmo.Application.Shared.Access;
using BA.Dmo.Domain.Shared.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Peso;

/// <summary>
/// Peso Operador route (Plan-V3 GLM-ACC-05, UD-06/UD-15): the Operador
/// experience belongs to holders of the peso module WITHOUT peso.aprovar.
/// The route guard enforces module entry server-side; the exclusivity guard
/// then redirects peso.aprovar holders to the Responsável experience —
/// no manual selector, no cross-exposure, capability-driven only.
/// The module content belongs to its own roadmap unit (U-10).
/// </summary>
public class IndexModel : PageModel
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public IndexModel(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public IActionResult OnGet()
    {
        var user = _currentUserAccessor.Current;
        // Fail closed: the module policy already proved a resolved identity,
        // but absence of one never renders module content.
        if (user is null || !user.HasModule(CanonicalModuleCatalog.PesoModuleId))
            return Forbid();

        // Responsável never receives the Operador page (GLM-ACC-05.2).
        if (user.HasCapability(CanonicalModuleCatalog.PesoAprovarCapabilityId))
            return Redirect("/peso/responsavel");

        return Page();
    }
}
