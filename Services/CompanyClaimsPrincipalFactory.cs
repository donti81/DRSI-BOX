using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DRSIBOX.Models;
using System.Security.Claims;

namespace DRSIBOX.Services
{
    public class CompanyClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public CompanyClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (user.CompanyId.HasValue)
                identity.AddClaim(new Claim("CompanyId", user.CompanyId.Value.ToString()));
            return identity;
        }
    }
}
