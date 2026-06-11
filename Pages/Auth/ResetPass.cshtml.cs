using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DRSIBOX.Models;
using DRSIBOX.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace DRSIBOX.Pages.Auth
{
    [AllowAnonymous]
    public class ResetPassModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;

        public ResetPassModel(UserManager<ApplicationUser> userManager, IEmailService email)
        {
            _userManager = userManager;
            _email       = email;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-mail je obvezen.")]
            [EmailAddress]
            public string Email { get; set; } = "";
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                var token        = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var link         = Url.Page(
                    "/Auth/NewPass",
                    pageHandler: null,
                    values: new { email = Input.Email, token = encodedToken },
                    protocol: Request.Scheme)!;

                await _email.SendAsync(
                    to: Input.Email,
                    subject: "Ponastavitev gesla",
                    body: $"Kliknite na spodnjo povezavo za ponastavitev gesla (veljavna 2 uri):\n\n{link}\n\nČe niste zahtevali ponastavitve gesla, sporočilo prezrite.");
            }

            StatusMessage = "Če račun s tem e-mailom obstaja, boste prejeli sporočilo z navodili.";
            return Page();
        }
    }
}
