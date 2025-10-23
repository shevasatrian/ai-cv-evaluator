using ai_cv_evaluator.Data;
using ai_cv_evaluator.Data.Entities;
using ai_cv_evaluator.Models.Enums;
using ai_cv_evaluator.Services;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace ai_cv_evaluator.BackgroundJobs
{
    public class EvaluationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EvaluationWorker> _logger;
        private readonly IWebHostEnvironment _env;

        public EvaluationWorker(IServiceScopeFactory scopeFactory, ILogger<EvaluationWorker> logger, IWebHostEnvironment env)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _env = env;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Evaluation Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<AiEvaluationService>();

                var job = await db.EvaluationJobs
                    .Include(j => j.Result)
                    .Where(j => j.Status == JobStatus.Queued)
                    .OrderBy(j => j.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (job != null)
                {
                    _logger.LogInformation($"🧠 Processing job {job.Id} ({job.JobTitle})...");
                    job.Status = JobStatus.Processing;
                    await db.SaveChangesAsync(stoppingToken);

                    try
                    {
                        // ✅ Path dasar wwwroot
                        var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

                        // ✅ Baca CV file
                        var cvPath = Path.Combine(rootPath, job.CvFilePath);
                        if (!File.Exists(cvPath))
                            throw new FileNotFoundException($"CV file not found at {cvPath}");

                        // baca cv
                        string cvText;
                        if (Path.GetExtension(cvPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var document = PdfDocument.Open(cvPath))
                            {
                                var builder = new System.Text.StringBuilder();
                                foreach (var page in document.GetPages())
                                {
                                    builder.AppendLine(page.Text);
                                }
                                cvText = builder.ToString();
                            }
                        }
                        else
                        {
                            cvText = File.ReadAllText(cvPath);
                        }

                        // ✅ Baca Project file (jika ada)
                        string projectText = "(no project report provided)";
                        if (!string.IsNullOrWhiteSpace(job.ReportFilePath))
                        {
                            var reportPath = Path.Combine(rootPath, job.ReportFilePath);
                            if (File.Exists(reportPath))
                            {
                                if (Path.GetExtension(reportPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var document = PdfDocument.Open(reportPath))
                                    {
                                        var builder = new System.Text.StringBuilder();
                                        foreach (var page in document.GetPages())
                                        {
                                            builder.AppendLine(page.Text);
                                        }
                                        projectText = builder.ToString();
                                    }
                                }
                                else
                                {
                                    projectText = File.ReadAllText(reportPath);
                                }
                            }
                        }

                        // ✅ Kirim ke OpenAI
                        var aiResult = await aiService.EvaluateAsync(job.JobTitle, cvText, projectText, stoppingToken);

                        // ✅ Simpan hasil evaluasi
                        var result = new EvaluationResult
                        {
                            Id = Guid.NewGuid(),
                            JobId = job.Id,
                            CvMatchRate = aiResult.Cv_Match_Rate,
                            CvFeedback = aiResult.Cv_Feedback,
                            ProjectScore = aiResult.Project_Score,
                            ProjectFeedback = aiResult.Project_Feedback,
                            OverallSummary = aiResult.Overall_Summary,
                            CreatedAt = DateTime.UtcNow
                        };

                        db.EvaluationResults.Add(result);
                        job.Result = result;
                        job.Status = JobStatus.Completed;
                        job.UpdatedAt = DateTime.UtcNow;

                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation($"✅ Job {job.Id} completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error while processing job in EvaluationWorker");
                        job.Status = JobStatus.Failed;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
