using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Starterkit.Models;

namespace Starterkit.Pages.Auth
{
    public class SignOutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SignOutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Auth/SignIn");
        }

        public IActionResult OnGet() => RedirectToPage("/Auth/SignIn");
    }
}
