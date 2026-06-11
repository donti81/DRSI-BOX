using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Starterkit.Models;
using System.ComponentModel.DataAnnotations;

namespace Starterkit.Pages.Auth
{
    public class SignInModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; private set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            public string Password { get; set; } = "";

            public bool RememberMe { get; set; }
        }

        public SignInModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid) return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return LocalRedirect(returnUrl ?? "/");

            ErrorMessage = "Invalid email or password.";
            return Page();
        }
    }
}
