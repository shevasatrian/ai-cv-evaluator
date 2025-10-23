namespace ai_cv_evaluator.Services
{
    public class FileStorageService
    {
        private readonly string _storagePath;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
        {
            // gunakan WebRootPath agar masuk ke wwwroot
            _storagePath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "Uploads");

            // pastikan folder wwwroot/Uploads dibuat jika belum ada
            Directory.CreateDirectory(_storagePath);

            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(_storagePath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File saved to: {Path}", fullPath);

            // return path relatif agar bisa diakses via URL, misalnya /Uploads/file.pdf
            return Path.Combine("Uploads", fileName).Replace("\\", "/");
        }
    }
}
