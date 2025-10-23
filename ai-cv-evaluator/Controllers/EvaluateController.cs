using ai_cv_evaluator.Data;
using ai_cv_evaluator.Models.DTOs;
using ai_cv_evaluator.Models.Enums;
using ai_cv_evaluator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ai_cv_evaluator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvaluateController : ControllerBase
    {
        private readonly EvaluationService _service;
        private readonly AppDbContext _context;

        public EvaluateController(EvaluationService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Evaluate([FromBody] EvaluateRequest req)
        {
            var job = await _service.CreateJobAsync(req.JobTitle, req.CvId, req.ReportId);
            return Ok(new { id = job.Id, status = job.Status.ToString().ToLower() });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvaluation(Guid id)
        {
            var job = await _context.EvaluationJobs
                .Include(j => j.Result)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
                return NotFound();

            var dto = new EvaluationJobDto(
                job.Id,
                job.JobTitle,
                job.CvFilePath,
                job.Status.ToString(),
                job.CreatedAt,
                job.Result == null ? null :
                    new EvaluationResultDto(
                        job.Result.CvMatchRate,
                        job.Result.CvFeedback,
                        job.Result.ProjectScore,
                        job.Result.ProjectFeedback,
                        job.Result.OverallSummary
                    )
            );

            return Ok(dto);
        }
    }
}
