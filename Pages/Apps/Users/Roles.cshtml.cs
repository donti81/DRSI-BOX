using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DRSIBOX.Models;

namespace Paces.Pages.Apps.Users
{
    [Authorize(Policy = "RequireAdmin")]
    public class RolesModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public record RoleViewModel(string Name, int UserCount);

        public List<RoleViewModel> Roles { get; set; } = [];
        public List<ApplicationUser> AllUsers { get; set; } = [];
        public Dictionary<string, string> UserRoles { get; set; } = [];

        [BindProperty] public string TargetUserId { get; set; } = "";
        [BindProperty] public string TargetRole { get; set; } = "";
        [BindProperty] public string NewRoleName { get; set; } = "";

        public string? StatusMessage { get; set; }

        public RolesModel(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task OnGetAsync(string? msg = null)
        {
            StatusMessage = msg;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAddRoleAsync()
        {
            var name = NewRoleName.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return RedirectToPage(new { msg = "Ime vloge je obvezno." });

            if (await _roleManager.RoleExistsAsync(name))
                return RedirectToPage(new { msg = $"Vloga '{name}' že obstaja." });

            await _roleManager.CreateAsync(new IdentityRole(name));
            return RedirectToPage(new { msg = $"Vloga '{name}' dodana." });
        }

        public async Task<IActionResult> OnPostAssignRoleAsync()
        {
            var user = await _userManager.FindByIdAsync(TargetUserId);
            if (user is null) return NotFound();

            var current = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, current);

            if (!string.IsNullOrWhiteSpace(TargetRole))
                await _userManager.AddToRoleAsync(user, TargetRole);

            await _userManager.UpdateSecurityStampAsync(user);
            return RedirectToPage(new { msg = "Vloga shranjena." });
        }

        private async Task LoadAsync()
        {
            AllUsers = await _userManager.Users.Include(u => u.Company).OrderBy(u => u.Email).ToListAsync();

            foreach (var user in AllUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UserRoles[user.Id] = roles.FirstOrDefault() ?? "";
            }

            Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList()
                .Select(r => new RoleViewModel(
                    r.Name!,
                    AllUsers.Count(u => UserRoles.TryGetValue(u.Id, out var ur) && ur == r.Name)
                )).ToList();
        }
    }
}
