// Data/Entities/DocumentVector.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace ai_cv_evaluator.Data.Entities
{
    public class DocumentVector
    {
        [Key]
        public Guid Id { get; set; }
        public string Source { get; set; } = ""; // e.g., "job_description", "case_study", "scoring_rubric"
        public string ChunkId { get; set; } = "";
        public string Text { get; set; } = "";
        public string EmbeddingJson { get; set; } = ""; // JSON array of floats
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
