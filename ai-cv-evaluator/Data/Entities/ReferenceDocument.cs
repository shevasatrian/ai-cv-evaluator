namespace ai_cv_evaluator.Data.Entities
{
    public class ReferenceDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Content { get; set; } = "";
        public string? VectorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
