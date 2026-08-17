using BA.Dmo.Application.Modules.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Admin.Users;

/// <summary>
/// Edit internal user (04_ACC §9): display name/profile title (display-only
/// text — UD-02), template assignment, activation and the explicit password
/// reset initiation. Optimistic concurrency via the posted version
/// (GLM-ACC-12); self-lockout protection is enforced in the use cases.
/// </summary>
public class EditModel : PageModel
{
    private readonly AdminUserService _users;
    private readonly AdminTemplateService _templates;

    public EditModel(AdminUserService users, AdminTemplateService templates)
    {
        _users = users;
        _templates = templates;
    }

    public AdminUserRow? Entry { get; private set; }

    public IReadOnlyList<AdminTemplateRow> Templates { get; private set; } = [];

    public string? Feedback { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        await LoadAsync(id);
        if (Entry is null)
            return Page();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(
        string id, string displayName, string? profileTitle, string templateId,
        bool active, string version)
    {
        if (!DateTimeOffset.TryParse(version, out var expectedVersion))
        {
            ModelState.AddModelError(string.Empty, "Versão de concorrência inválida.");
            await LoadAsync(id);
            return Page();
        }

        var result = await _users.SaveUserAsync(
            id, displayName, profileTitle, templateId, active, expectedVersion,
            HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await LoadAsync(id);
            return Page();
        }

        return Redirect("/admin/users");
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(string id)
    {
        var result = await _users.RequestPasswordResetAsync(id, HttpContext.RequestAborted);

        await LoadAsync(id);
        Feedback = result.IsSuccess
            ? "Reset de palavra-passe iniciado."
            : result.Error.Message;
        if (result.IsFailure)
            ModelState.AddModelError(string.Empty, result.Error.Message);

        return Page();
    }

    private async Task LoadAsync(string id)
    {
        Templates = await _templates.ListAsync(HttpContext.RequestAborted);
        var user = await _users.GetAsync(id, HttpContext.RequestAborted);
        Entry = user.IsSuccess ? user.Value : null;
    }
}
