using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Starterkit.Data;
using Starterkit.Models;
using System.ComponentModel.DataAnnotations;

namespace Starterkit.Pages.Admin.Companies
{
    [Authorize(Policy = "RequireAdmin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public record CompanyViewModel(int Id, string Name, string? TaxNumber, string? Naslov, int? Posta, string? PostaNaziv, int UserCount);

        public List<CompanyViewModel> Companies { get; set; } = [];

        [BindProperty] public int EditId { get; set; }
        [BindProperty, Required, MaxLength(200)] public string EditName { get; set; } = "";
        [BindProperty, MaxLength(50)] public string? EditTaxNumber { get; set; }
        [BindProperty, MaxLength(60)] public string? EditNaslov { get; set; }
        [BindProperty] public int? EditPosta { get; set; }
        [BindProperty, MaxLength(60)] public string? EditPostaNaziv { get; set; }

        [BindProperty, Required, MaxLength(200)] public string NewCompanyName { get; set; } = "";
        [BindProperty, Required, MaxLength(50)] public string NewCompanyTaxNumber { get; set; } = "";
        [BindProperty, MaxLength(60)] public string? NewNaslov { get; set; }
        [BindProperty] public int? NewPosta { get; set; }
        [BindProperty, MaxLength(60)] public string? NewPostaNaziv { get; set; }

        [BindProperty] public int DeleteId { get; set; }

        public string? StatusMessage { get; set; }

        public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task OnGetAsync(string? msg = null)
        {
            StatusMessage = msg;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var name = NewCompanyName.Trim();
            if (await _db.Companies.FirstOrDefaultAsync(c => c.Name == name) is not null)
                return new JsonResult(new { success = false, message = $"Podjetje '{name}' že obstaja." });

            if (string.IsNullOrWhiteSpace(NewCompanyTaxNumber))
                return new JsonResult(new { success = false, message = "Davčna številka je obvezna." });

            var tax = NewCompanyTaxNumber.Trim();
            if (await _db.Companies.FirstOrDefaultAsync(c => c.TaxNumber == tax) is not null)
                return new JsonResult(new { success = false, message = $"Podjetje z davčno številko '{tax}' že obstaja." });

            _db.Companies.Add(new Company
            {
                Name = name,
                TaxNumber = tax,
                NASLOV = string.IsNullOrWhiteSpace(NewNaslov) ? null : NewNaslov.Trim(),
                POSTA = NewPosta,
                POSTA_NAZIV = string.IsNullOrWhiteSpace(NewPostaNaziv) ? null : NewPostaNaziv.Trim()
            });
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, message = $"Podjetje '{name}' dodano." });
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var company = await _db.Companies.FindAsync(EditId);
            if (company is null) return NotFound();

            var name = EditName.Trim();
            if (await _db.Companies.FirstOrDefaultAsync(c => c.Name == name && c.Id != EditId) is not null)
                return new JsonResult(new { success = false, message = $"Podjetje '{name}' že obstaja." });

            if (!string.IsNullOrWhiteSpace(EditTaxNumber))
            {
                var editTax = EditTaxNumber.Trim();
                if (await _db.Companies.FirstOrDefaultAsync(c => c.TaxNumber == editTax && c.Id != EditId) is not null)
                    return new JsonResult(new { success = false, message = $"Podjetje z davčno številko '{editTax}' že obstaja." });
            }

            company.Name = name;
            company.TaxNumber = string.IsNullOrWhiteSpace(EditTaxNumber) ? null : EditTaxNumber.Trim();
            company.NASLOV = string.IsNullOrWhiteSpace(EditNaslov) ? null : EditNaslov.Trim();
            company.POSTA = EditPosta;
            company.POSTA_NAZIV = string.IsNullOrWhiteSpace(EditPostaNaziv) ? null : EditPostaNaziv.Trim();
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, message = $"Podjetje '{name}' posodobljeno." });
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var company = await _db.Companies.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == DeleteId);
            if (company is null) return NotFound();

            if (company.Users.Any())
                return RedirectToPage(new { msg = $"Podjetja '{company.Name}' ni mogoče izbrisati — ima {company.Users.Count} uporabnikov." });

            _db.Companies.Remove(company);
            await _db.SaveChangesAsync();
            return RedirectToPage(new { msg = $"Podjetje '{company.Name}' izbrisano." });
        }

        private async Task LoadAsync()
        {
            var companies = await _db.Companies.Include(c => c.Users).OrderBy(c => c.Name).ToListAsync();
            Companies = companies.Select(c => new CompanyViewModel(c.Id, c.Name, c.TaxNumber, c.NASLOV, c.POSTA, c.POSTA_NAZIV, c.Users.Count)).ToList();
        }
    }
}
