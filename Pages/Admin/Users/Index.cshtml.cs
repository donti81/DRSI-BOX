using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Starterkit.Data;
using Starterkit.Models;
using System.ComponentModel.DataAnnotations;

namespace Starterkit.Pages.Admin.Users
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roleManager;

        public List<ApplicationUser> Users { get; set; } = [];
        public List<Company> Companies { get; set; } = [];
        public List<string> AvailableRoles { get; set; } = [];
        public Dictionary<string, string> UserRoles { get; set; } = [];

        [BindProperty] public string UserId { get; set; } = "";
        [BindProperty] public string AssignRoleUserId { get; set; } = "";
        [BindProperty] public string AssignRoleName { get; set; } = "";
        [BindProperty] public string NewUserEmail { get; set; } = "";
        [BindProperty] public string? NewUserFullName { get; set; }
        [BindProperty] public string NewUserPassword { get; set; } = "";
        [BindProperty] public string? NewUserRole { get; set; }
        [BindProperty] public int? NewUserCompanyId { get; set; }

        [BindProperty]
        public int? CompanyId { get; set; }

        [BindProperty]
        [Required, MaxLength(200)]
        public string NewCompanyName { get; set; } = "";

        [BindProperty]
        [MaxLength(50)]
        public string? NewCompanyTaxNumber { get; set; }

        public string? StatusMessage { get; set; }

        public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _db = db;
            _roleManager = roleManager;
        }

        public async Task OnGetAsync(string? msg = null)
        {
            StatusMessage = msg;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAssignCompanyAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user is null) return NotFound();

            user.CompanyId = CompanyId;
            await _userManager.UpdateAsync(user);
            // Invalidate security stamp so cookie is refreshed on next validation cycle
            await _userManager.UpdateSecurityStampAsync(user);

            return RedirectToPage(new { msg = "Podjetje shranjeno." });
        }

        public async Task<IActionResult> OnPostAssignRoleAsync()
        {
            var user = await _userManager.FindByIdAsync(AssignRoleUserId);
            if (user is null) return NotFound();

            var current = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, current);

            if (!string.IsNullOrWhiteSpace(AssignRoleName))
                await _userManager.AddToRoleAsync(user, AssignRoleName);

            await _userManager.UpdateSecurityStampAsync(user);
            return RedirectToPage(new { msg = "Vloga shranjena." });
        }

        public async Task<IActionResult> OnPostAddUserAsync()
        {
            var email = (NewUserEmail ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email))
                return new JsonResult(new { success = false, message = "E-mail je obvezen." });

            if (string.IsNullOrWhiteSpace(NewUserPassword))
                return new JsonResult(new { success = false, message = "Geslo je obvezno." });

            if (string.IsNullOrWhiteSpace(NewUserRole))
                return new JsonResult(new { success = false, message = "Vloga je obvezna." });

            if (await _userManager.FindByEmailAsync(email) is not null)
                return new JsonResult(new { success = false, message = $"Uporabnik z e-mailom '{email}' že obstaja." });

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = string.IsNullOrWhiteSpace(NewUserFullName) ? null : NewUserFullName.Trim(),
                CompanyId = NewUserCompanyId,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, NewUserPassword ?? "");
            if (!result.Succeeded)
                return new JsonResult(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            await _userManager.AddToRoleAsync(user, NewUserRole);

            return new JsonResult(new { success = true, message = $"Uporabnik {email} dodan." });
        }

        public async Task<IActionResult> OnPostAddCompanyAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            _db.Companies.Add(new Company
            {
                Name = NewCompanyName.Trim(),
                TaxNumber = string.IsNullOrWhiteSpace(NewCompanyTaxNumber) ? null : NewCompanyTaxNumber.Trim()
            });
            await _db.SaveChangesAsync();

            return RedirectToPage(new { msg = $"Podjetje \"{NewCompanyName.Trim()}\" dodano." });
        }

        private async Task LoadAsync()
        {
            Users = await _db.Users.Include(u => u.Company).OrderBy(u => u.Email).ToListAsync();
            Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
            AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync();

            foreach (var user in Users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UserRoles[user.Id] = roles.FirstOrDefault() ?? "";
            }
        }
    }
}
