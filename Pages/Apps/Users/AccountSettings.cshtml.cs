using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DRSIBOX.Models;
using System.ComponentModel.DataAnnotations;

namespace Paces.Pages.Apps.Users
{
    [Authorize]
    public class AccountSettingsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }

        public class InputModel
        {
            public string? FullName { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; } = "";

            public string? CurrentPassword { get; set; }

            [MinLength(8)]
            public string? NewPassword { get; set; }
        }

        public AccountSettingsModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnGetAsync(string? msg = null)
        {
            StatusMessage = msg;
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return;

            Input = new InputModel
            {
                FullName = user.FullName,
                Email = user.Email ?? ""
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            user.FullName = Input.FullName?.Trim();

            if (user.Email != Input.Email.Trim())
            {
                user.Email = Input.Email.Trim();
                user.UserName = Input.Email.Trim();
                await _userManager.UpdateNormalizedEmailAsync(user);
                await _userManager.UpdateNormalizedUserNameAsync(user);
            }

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(Input.NewPassword) && !string.IsNullOrWhiteSpace(Input.CurrentPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return Page();
                }
                await _userManager.UpdateSecurityStampAsync(user);
            }

            return RedirectToPage(new { msg = "Nastavitve shranjene." });
        }
    }
}
