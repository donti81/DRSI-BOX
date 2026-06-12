using DRSIBOX.Models;

namespace DRSIBOX.Services
{
    public interface IUploadPathResolver
    {
        string Root { get; }
        string Resolve(UploadLog file);
    }

    public class UploadPathResolver : IUploadPathResolver
    {
        public string Root { get; }

        public UploadPathResolver(IConfiguration config, IWebHostEnvironment env)
        {
            var configured = config.GetValue<string>("UploadRootPath") ?? "uploads";
            Root = Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(env.WebRootPath, configured);
        }

        public string Resolve(UploadLog file) =>
            string.IsNullOrEmpty(file.Folder)
                ? Path.Combine(Root, file.FileName)
                : Path.Combine(Root, file.Folder, file.FileName);
    }
}
