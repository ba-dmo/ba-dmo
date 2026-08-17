using System.Text;
using BA.Dmo.Application.Modules.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Admin.Audit;

/// <summary>
/// Auditoria tab (04_ACC §9, UD-17/TD-19): factual annual registry with
/// filters by year/user/module/action/result and canonical pagination
/// 20/40/60. Viewing requires audit.view; the export handler requires
/// audit.export (re-checked in the use case). Read-only: no scores, no
/// rankings, no evaluation — facts only.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AdminAuditService _audit;

    public IndexModel(AdminAuditService audit)
    {
        _audit = audit;
    }

    public int? Year { get; private set; }
    public string? Actor { get; private set; }
    public string? Module { get; private set; }
    public string? Action { get; private set; }
    public string? Result { get; private set; }
    public int PageSize { get; private set; } = 20;

    public AuditQueryResult? Events { get; private set; }

    public async Task OnGetAsync(int? year, string? actor, string? module,
        string? action, string? result, int pageSize = 20, int page = 1)
    {
        Year = year;
        Actor = actor;
        Module = module;
        Action = action;
        Result = result;
        PageSize = pageSize;

        var query = await _audit.QueryAsync(BuildFilter(page), HttpContext.RequestAborted);
        if (query.IsFailure)
        {
            ModelState.AddModelError(string.Empty, query.Error.Message);
            return;
        }

        Events = query.Value;
    }

    public async Task<IActionResult> OnPostExportAsync(int? year, string? actor,
        string? module, string? action, string? result, int pageSize = 20, int page = 1)
    {
        var export = await _audit.ExportAsync(BuildFilter(page), HttpContext.RequestAborted);
        if (export.IsFailure)
        {
            ModelState.AddModelError(string.Empty, export.Error.Message);
            await OnGetAsync(year, actor, module, action, result, pageSize, page);
            return Page();
        }

        return File(
            Encoding.UTF8.GetBytes(export.Value),
            "text/csv",
            $"auditoria-{Year?.ToString() ?? "tudo"}.csv");
    }

    private AuditQueryFilter BuildFilter(int page) => new(
        Year,
        string.IsNullOrWhiteSpace(Actor) ? null : Actor,
        string.IsNullOrWhiteSpace(Module) ? null : Module,
        string.IsNullOrWhiteSpace(Action) ? null : Action,
        string.IsNullOrWhiteSpace(Result) ? null : Result,
        FromUtc: null,
        ToUtc: null,
        Page: page,
        PageSize: PageSize);
}
