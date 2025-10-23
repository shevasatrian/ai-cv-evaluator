namespace ai_cv_evaluator.Models.DTOs
{
    public record EvaluationResultDto(
        double CvMatchRate,
        string CvFeedback,
        double ProjectScore,
        string ProjectFeedback,
        string OverallSummary
    );
}
