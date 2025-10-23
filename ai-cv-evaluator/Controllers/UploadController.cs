using ai_cv_evaluator.Models.DTOs;
using ai_cv_evaluator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ai_cv_evaluator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {

        private readonly FileStorageService _storage;

        public UploadController(FileStorageService storage)
        {
            _storage = storage;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadRequest req)
        {
            if (req.Cv == null || req.Report == null)
                return BadRequest("Both CV and Report files are required.");

            var cvId = await _storage.SaveFileAsync(req.Cv);
            var reportId = await _storage.SaveFileAsync(req.Report);

            return Ok(new { cvId, reportId });
        }
    }
}
