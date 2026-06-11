using System.ComponentModel.DataAnnotations;

namespace Starterkit.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? TaxNumber { get; set; }

        [MaxLength(60)] public string? NASLOV { get; set; }
        public int? POSTA { get; set; }
        [MaxLength(60)] public string? POSTA_NAZIV { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = [];
    }
}
