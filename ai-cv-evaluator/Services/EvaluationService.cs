using ai_cv_evaluator.Data;
using ai_cv_evaluator.Data.Entities;
using ai_cv_evaluator.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ai_cv_evaluator.Services
{
    public class EvaluationService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<EvaluationService> _logger;

        public EvaluationService(AppDbContext db, ILogger<EvaluationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EvaluationJob> CreateJobAsync(string jobTitle, string cvFile, string reportFile)
        {
            var job = new EvaluationJob
            {
                JobTitle = jobTitle,
                CvFilePath = cvFile,
                ReportFilePath = reportFile,
                Status = JobStatus.Queued,
                CreatedAt = DateTime.UtcNow
            };

            _db.EvaluationJobs.Add(job);
            await _db.SaveChangesAsync();
            return job;
        }

        public async Task<EvaluationJob?> GetJobAsync(Guid id)
        {
            return await _db.EvaluationJobs
                .Include(j => j.Result)
                .FirstOrDefaultAsync(j => j.Id == id);
        }
    }
}
