using ai_cv_evaluator.Models.Enums;

namespace ai_cv_evaluator.Data.Entities
{
    public class EvaluationJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string JobTitle { get; set; } = "";
        public string CvFilePath { get; set; } = "";
        public string ReportFilePath { get; set; } = "";
        public JobStatus Status { get; set; } = JobStatus.Queued;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public EvaluationResult? Result { get; set; }
    }
}
