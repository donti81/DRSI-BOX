using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Starterkit.Models;

namespace Paces.Pages.Apps.Users
{
    [Authorize(Policy = "RequireAdmin")]
    public class RoleDetailsModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public string RoleName { get; set; } = "";
        public List<ApplicationUser> RoleUsers { get; set; } = [];
        public List<ApplicationUser> AvailableUsers { get; set; } = [];

        [BindProperty] public string RemoveUserId { get; set; } = "";
        [BindProperty] public string AddUserId { get; set; } = "";
        [BindProperty] public string CurrentRole { get; set; } = "";

        public string? StatusMessage { get; set; }

        public RoleDetailsModel(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(string? role = null, string? msg = null)
        {
            StatusMessage = msg;
            if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
                return RedirectToPage("Roles");

            RoleName = role;
            await LoadAsync(role);
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveUserAsync()
        {
            var user = await _userManager.FindByIdAsync(RemoveUserId);
            if (user is not null)
            {
                await _userManager.RemoveFromRoleAsync(user, CurrentRole);
                await _userManager.UpdateSecurityStampAsync(user);
            }
            return RedirectToPage(new { role = CurrentRole, msg = "Uporabnik odstranjen iz vloge." });
        }

        public async Task<IActionResult> OnPostAddUserAsync()
        {
            var user = await _userManager.FindByIdAsync(AddUserId);
            if (user is not null)
            {
                var current = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, current);
                await _userManager.AddToRoleAsync(user, CurrentRole);
                await _userManager.UpdateSecurityStampAsync(user);
            }
            return RedirectToPage(new { role = CurrentRole, msg = "Uporabnik dodan v vlogo." });
        }

        private async Task LoadAsync(string role)
        {
            var inRole = await _userManager.GetUsersInRoleAsync(role);
            RoleUsers = inRole.OrderBy(u => u.Email).ToList();

            var all = await _userManager.Users.ToListAsync();
            AvailableUsers = all.Where(u => inRole.All(r => r.Id != u.Id))
                               .OrderBy(u => u.Email).ToList();
        }
    }
}
