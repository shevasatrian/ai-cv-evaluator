namespace ai_cv_evaluator.Models.DTOs
{
    public record EvaluationJobDto(
        Guid Id,
        string JobTitle,
        string CvFilePath,
        string Status,
        DateTime CreatedAt,
        EvaluationResultDto? Result
    );
}
