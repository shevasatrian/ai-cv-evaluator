using ai_cv_evaluator.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ai_cv_evaluator.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<EvaluationJob> EvaluationJobs { get; set; }
        public DbSet<EvaluationResult> EvaluationResults { get; set; }
        public DbSet<ReferenceDocument> ReferenceDocuments { get; set; }
        public DbSet<DocumentVector> DocumentVectors { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvaluationJob>()
                .HasOne(j => j.Result)
                .WithOne(r => r.Job)
                .HasForeignKey<EvaluationResult>(r => r.JobId);
        }
    }
}
