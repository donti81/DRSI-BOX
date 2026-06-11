using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Starterkit.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Starterkit.Pages.Auth
{
    [AllowAnonymous]
    public class NewPassModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public NewPassModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            public string Token { get; set; } = "";

            [Required(ErrorMessage = "Geslo je obvezno.")]
            [MinLength(8, ErrorMessage = "Geslo mora imeti vsaj 8 znakov.")]
            public string Password { get; set; } = "";

            [Required(ErrorMessage = "Potrditev gesla je obvezna.")]
            [Compare(nameof(Password), ErrorMessage = "Gesli se ne ujemata.")]
            public string ConfirmPassword { get; set; } = "";
        }

        public IActionResult OnGet(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToPage("/Auth/ResetPass");

            Input.Email = email;
            Input.Token = WebUtility.UrlDecode(token);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Neveljaven ali potekel zahtevek.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.Password);
            if (result.Succeeded)
                return RedirectToPage("/Auth/SignIn");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return Page();
        }
    }
}
