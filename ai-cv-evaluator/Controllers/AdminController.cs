using ai_cv_evaluator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace ai_cv_evaluator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly RAGService _rag;
        private readonly ILogger<AdminController> _logger;

        public AdminController(RAGService rag, ILogger<AdminController> logger)
        {
            _rag = rag;
            _logger = logger;
        }

        /// <summary>
        /// Upload dokumen referensi (Job Description, Case Study, Rubric, dsb)
        /// </summary>
        // <param name="source">Key dokumen (contoh: job_description, case_study, cv_rubric, project_rubric)</param>
        // <param name="file">File PDF atau TXT</param>
        [HttpPost("ingest")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Ingest([FromQuery] string source, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(source))
                return BadRequest("Parameter 'source' wajib diisi. Contoh: job_description, case_study, cv_rubric, project_rubric.");

            if (file == null || file.Length == 0)
                return BadRequest("File tidak boleh kosong.");

            string text;

            // 🔹 Ekstraksi teks dari PDF atau TXT
            if (Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = file.OpenReadStream();
                using var document = PdfDocument.Open(stream);
                var sb = new System.Text.StringBuilder();

                foreach (var page in document.GetPages())
                    sb.AppendLine(page.Text);

                text = sb.ToString();
            }
            else
            {
                using var reader = new StreamReader(file.OpenReadStream());
                text = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("File tidak berisi teks yang valid.");

            await _rag.IngestDocumentAsync(source, text, HttpContext.RequestAborted);

            _logger.LogInformation("✅ Ingest berhasil untuk {Source}", source);
            return Ok(new { message = "Ingest sukses", source });
        }
    }
}
