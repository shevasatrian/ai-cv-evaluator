namespace ai_cv_evaluator.Models.DTOs
{
    public class EvaluateRequest
    {
        public string JobTitle { get; set; } = "";
        public string CvId { get; set; } = "";
        public string ReportId { get; set; } = "";
    }
}
