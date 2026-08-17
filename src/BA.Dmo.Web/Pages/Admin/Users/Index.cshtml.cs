using BA.Dmo.Application.Modules.Admin;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BA.Dmo.Web.Pages.Admin.Users;

/// <summary>User listing (04_ACC §9: listar/pesquisar). Read-only view.</summary>
public class IndexModel : PageModel
{
    private readonly AdminUserService _users;

    public IndexModel(AdminUserService users)
    {
        _users = users;
    }

    public IReadOnlyList<AdminUserRow> Users { get; private set; } = [];

    public string? Search { get; private set; }

    public async Task OnGetAsync(string? q)
    {
        Search = q;
        Users = await _users.ListAsync(q, HttpContext.RequestAborted);
    }
}
