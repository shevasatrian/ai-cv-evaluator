namespace ai_cv_evaluator.Data.Entities
{
    public class EvaluationResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public double CvMatchRate { get; set; }
        public string CvFeedback { get; set; } = "";
        public double ProjectScore { get; set; }
        public string ProjectFeedback { get; set; } = "";
        public string OverallSummary { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relasi ke Job
        public Guid JobId { get; set; }
        public EvaluationJob? Job { get; set; }
    }
}
