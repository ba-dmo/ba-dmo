using BA.Dmo.Application.Modules.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Admin.Applications;

/// <summary>
/// Catalog mirror administration (04_ACC §9 "Aplicações", GLM-CAT-02 rule 3):
/// display order and activation of KNOWN catalog modules only. Unknown
/// identifiers cannot be created; the mirror never influences authorization.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AdminMirrorService _mirror;

    public IndexModel(AdminMirrorService mirror)
    {
        _mirror = mirror;
    }

    public sealed class EntryLine
    {
        public string ModuleId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool Active { get; set; } = true;
    }

    public List<EntryLine> Entries { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(List<MirrorEntryInput> entries)
    {
        var result = await _mirror.SaveDisplayAsync(
            entries ?? new List<MirrorEntryInput>(), HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await LoadAsync();
            return Page();
        }

        return Redirect("/admin/applications");
    }

    private async Task LoadAsync()
    {
        var display = await _mirror.GetDisplayAsync(HttpContext.RequestAborted);
        Entries = display.IsSuccess
            ? display.Value.Select(e => new EntryLine
            {
                ModuleId = e.Module.ModuleId,
                DisplayName = e.Module.DisplayName,
                DisplayOrder = e.DisplayOrder,
                Active = e.Active
            }).ToList()
            : new List<EntryLine>();
    }
}
