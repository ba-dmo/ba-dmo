using BA.Dmo.Application.Shared.Access;
using BA.Dmo.Domain.Shared.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Peso;

/// <summary>
/// Peso Responsável route (Plan-V3 GLM-ACC-05, UD-06/UD-15): the Responsável
/// experience belongs to holders of the peso module WITH peso.aprovar. The
/// route guard enforces module entry server-side; the exclusivity guard then
/// redirects users without peso.aprovar to the Operador experience — no
/// manual selector, no cross-exposure, capability-driven only. Decision
/// commands (aprovar/nao_aprovar/reabrir) validate peso.aprovar inside the
/// use cases (GLM-ACC-05.4) when the module unit lands (U-10).
/// </summary>
public class ResponsavelModel : PageModel
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public ResponsavelModel(ICurrentUserAccessor currentUserAccessor)
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

        // Operador never accesses Responsável routes/commands (GLM-ACC-05.2).
        if (!user.HasCapability(CanonicalModuleCatalog.PesoAprovarCapabilityId))
            return Redirect("/peso");

        return Page();
    }
}
