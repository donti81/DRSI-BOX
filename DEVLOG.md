# Development Log — Starterkit (Paces Admin Dashboard)

## 2026-05-08

### Inicializacija projekta
- Ustvarjen `CLAUDE.md` z dokumentacijo arhitekture projekta
- Nameščen Node.js (winget) — npm ni bil v PATH
- Nameščeni manjkajoči npm paketi (19 CSS paketov manjkalo v `package.json`)
- Popravek: `cropperjs@2` nima `dist/cropper.min.css` → downgrade na `cropperjs@1`

### FilePond console errors
- Problem: `wwwroot/plugins/filepond/` ni bil ustvarjen
- Namestitev 3 manjkajočih filepond plugin paketov
- Dodan `dropzone` in `filepond` entry v `plugins.config.js`
- Gulp build skopiral plugine v `wwwroot/plugins/`

### File Upload handler
- Ustvarjen `Pages/Form/Fileuploads.cshtml.cs` z `OnPostUploadAsync()`
- Dropzone in FilePond formama dodan CSRF token (`RequestVerificationToken` header)
- Datoteke se shranjujejo v `wwwroot/uploads/`

### Oracle 21c — Upload logging
- Dodan `Oracle.ManagedDataAccess.Core 23.26.200`
- Ustvarjena `upload_logs` tabela v Oracle XE 21c (schema `ITROHA`)
- Ustvarjen `Models/UploadLog.cs`, `Services/IUploadLogService.cs`, `Services/UploadLogService.cs`
- Vsak upload se logira: file_name, original_name, content_type, file_size, uploaded_at, ip_address
- Fix Oracle listener: `LOCAL_LISTENER` je kazal na stari IP → `ALTER SYSTEM SET LOCAL_LISTENER='...'`

### Resumable uploads — tusdotnet + Uppy v3
- Problem: Kestrel default limit ~28.6 MB, brez podpore za prekinitve
- Dodan NuGet `tusdotnet 2.11.1`
- Nameščen npm `uppy@3` (UMD bundle ~600 KB)
- Dodan `uppy` entry v `plugins.config.js`
- `Program.cs`: `UseTus` middleware na `/tus`, `TusDiskStore` v `ContentRoot/tus-store/`
- `OnFileCompleteAsync`: premik datoteke v `wwwroot/uploads/`, brisanje `.info` sidecar, Oracle log
- Klient: Uppy Dashboard + Tus plugin (10 MB chunks, retry delays, `removeFingerprintOnSuccess`)
- Antiforgery **ni potreben** za `/tus` — middleware se izvaja pred Razor Pages

### FileManager — dinamični prikaz
- Problem: `Pages/Apps/FileManager.cshtml` — statična stran z 8 hardkodiranimi mapami in 9 vrsticami
- Dodani `GetAllAsync()` in `DeleteAsync(long id)` v `IUploadLogService` / `UploadLogService`
- `FileManagerModel`: `OnGetAsync()` bere iz Oracle, `OnPostDeleteAsync(long id)` briše disk + DB
- Cshtml: `@foreach` po `Model.Files`, download link z `download="@file.OriginalName"`, delete form z antiforgery
- Gumb "Upload Files" preusmerjen na `/form/fileuploads`
- Prikazani zadnji 4 uploadi kot "Recent Files" kartice
- Odstranjene hardkodirane mape in loading spinner

### ASP.NET Identity + Oracle autentikacija
- Nameščeni: `Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.15`, `Oracle.EntityFrameworkCore 9.23.26200`
- Ustvarjen `Models/ApplicationUser.cs` (extends IdentityUser, doda FullName)
- Ustvarjen `Data/ApplicationDbContext.cs` (IdentityDbContext)
- **Fix Oracle 21c — bool**: EF Core 9 generira `BOOLEAN` ki ga Oracle 21c ne podpira → `ConfigureConventions` nastavi vse `bool` na `NUMBER(1)`
- **Fix EnsureCreated()**: Oracle preskoči kreacijo tabel kadar katerekoli tabele že obstajajo v shemi → zamenjano z eksplicitnim preverjanjem `AspNetUsers` in klicem `CreateTablesAsync()`
- `Program.cs`: Identity DI, cookie auth (LoginPath `/auth/sign-in`, 7 dni expiry), global `AuthorizeFolder("/")`, `AllowAnonymousToFolder("/Auth")`
- `UseAuthentication()` dodan pred `UseAuthorization()`
- Seeded testna uporabnika ob zagonu aplikacije
- `Pages/Auth/SignIn.cshtml.cs`: `SignInManager.PasswordSignInAsync`, redirect na `/` po prijavi
- `Pages/Auth/SignOut.cshtml.cs`: `SignOutAsync`, redirect na sign-in
- `_Topbar.cshtml`: dinamično ime/vloga iz `User.Identity`, logout form POST

### Testni uporabniki
| Email | Geslo | Vloga |
|---|---|---|
| admin@example.com | Admin1234! | Admin |
| user@example.com | User1234! | User |

### Oracle Identity tabele
Kreirane v shemi ITROHA:
`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`
