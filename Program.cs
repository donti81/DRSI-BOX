using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Starterkit.Data;
using Starterkit.Models;
using Starterkit.Services;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

var builder = WebApplication.CreateBuilder(args);

// EF Core + Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("Oracle")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 8;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<CompanyClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/auth/sign-in";
    options.LogoutPath       = "/auth/sign-out";
    options.AccessDeniedPath = "/auth/sign-in";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan   = TimeSpan.FromDays(7);
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Auth");
    options.Conventions.AllowAnonymousToFolder("/Dl");
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdmin");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromHours(
        builder.Configuration.GetValue<int>("ResetPassword:TokenLifespanHours", 2)));

builder.Services.AddScoped<IUploadLogService, UploadLogService>();
builder.Services.AddScoped<IDownloadLogService, DownloadLogService>();
builder.Services.AddScoped<IUploadNotificationService, UploadNotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDownloadTokenService, DownloadTokenService>();

var app = builder.Build();

// Create Identity tables + seed test users
await using (var scope = app.Services.CreateAsyncScope())
{
    var db      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Oracle EnsureCreated() skips table creation when any other tables exist in the schema.
    // Check specifically for AspNetUsers and create Identity tables only if missing.
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    await using var checkCmd = conn.CreateCommand();
    checkCmd.CommandText = "SELECT COUNT(*) FROM user_tables WHERE table_name = 'AspNetUsers'";
    var identityTableCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
    await conn.CloseAsync();

    if (identityTableCount == 0)
    {
        var creator = db.GetInfrastructure().GetRequiredService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync();
    }

    foreach (var role in new[] { "Admin", "User", "DRSI" })
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new IdentityRole(role));

    if (await userMgr.FindByEmailAsync("admin@example.com") is null)
    {
        var admin = new ApplicationUser
        {
            UserName       = "admin@example.com",
            Email          = "admin@example.com",
            EmailConfirmed = true,
            FullName       = "Admin User",
        };
        await userMgr.CreateAsync(admin, "Admin1234!");
        await userMgr.AddToRoleAsync(admin, "Admin");
    }

    if (await userMgr.FindByEmailAsync("user@example.com") is null)
    {
        var user = new ApplicationUser
        {
            UserName       = "user@example.com",
            Email          = "user@example.com",
            EmailConfirmed = true,
            FullName       = "Test User",
        };
        await userMgr.CreateAsync(user, "User1234!");
        await userMgr.AddToRoleAsync(user, "User");
    }

    if (await userMgr.FindByEmailAsync("igortroha@gmail.com") is null)
    {
        var itit = await db.Companies.FirstOrDefaultAsync(c => c.Name == "ITIT");
        if (itit is null)
        {
            itit = new Company { Name = "ITIT" };
            db.Companies.Add(itit);
            await db.SaveChangesAsync();
        }

        var igor = new ApplicationUser
        {
            UserName       = "igortroha@gmail.com",
            Email          = "igortroha@gmail.com",
            EmailConfirmed = true,
            FullName       = "Igor Troha",
            CompanyId      = itit.Id,
        };
        await userMgr.CreateAsync(igor, "Trohica-3");
        await userMgr.AddToRoleAsync(igor, "User");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

var tusDiskStorePath = Path.Combine(app.Environment.ContentRootPath, "tus-store");
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(tusDiskStorePath);
Directory.CreateDirectory(uploadsPath);

app.UseTus(httpContext => new DefaultTusConfiguration
{
    Store = new TusDiskStore(tusDiskStorePath),
    UrlPath = "/tus",
    MaxAllowedUploadSizeInBytesLong = null,
    Events = new Events
    {
        OnFileCompleteAsync = async ctx =>
        {
            var file = await ctx.GetFileAsync();
            var metadata = await file.GetMetadataAsync(ctx.CancellationToken);

            var originalName = metadata.TryGetValue("filename", out var fnMeta)
                ? fnMeta.GetString(System.Text.Encoding.UTF8) : ctx.FileId;
            var contentType = metadata.TryGetValue("filetype", out var ctMeta)
                ? ctMeta.GetString(System.Text.Encoding.UTF8) : null;
            var folder = metadata.TryGetValue("folder", out var folderMeta)
                ? folderMeta.GetString(System.Text.Encoding.UTF8) : null;

            var ext = Path.GetExtension(originalName);
            var storedFileName = $"{Guid.NewGuid()}{ext}";
            var sourcePath = Path.Combine(tusDiskStorePath, ctx.FileId);

            var destDir = string.IsNullOrWhiteSpace(folder)
                ? uploadsPath
                : Path.Combine(uploadsPath, folder);
            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, storedFileName);

            File.Move(sourcePath, destPath);

            var infoFile = sourcePath + ".info";
            if (File.Exists(infoFile)) File.Delete(infoFile);

            await using var asyncScope = ctx.HttpContext.RequestServices.CreateAsyncScope();
            var uploadLog = asyncScope.ServiceProvider.GetRequiredService<IUploadLogService>();

            await uploadLog.LogAsync(new UploadLog
            {
                FileName     = storedFileName,
                OriginalName = originalName,
                ContentType  = contentType,
                FileSize     = new FileInfo(destPath).Length,
                UploadedAt   = DateTime.UtcNow,
                IpAddress    = ctx.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UploadedBy   = ctx.HttpContext.User.Identity?.Name,
                Folder       = string.IsNullOrWhiteSpace(folder) ? null : folder,
            });
        }
    }
});

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
