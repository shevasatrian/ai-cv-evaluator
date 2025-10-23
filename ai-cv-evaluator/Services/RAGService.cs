using ai_cv_evaluator.Data;
using ai_cv_evaluator.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ai_cv_evaluator.Services
{
    public class RAGService
    {
        private readonly AppDbContext _db;
        private readonly EmbeddingService _embed;
        private readonly ILogger<RAGService> _logger;

        public RAGService(AppDbContext db, EmbeddingService embed, ILogger<RAGService> logger)
        {
            _db = db;
            _embed = embed;
            _logger = logger;
        }

        // Simple chunker: split by paragraphs or fixed char lengths to keep chunk sizes manageable
        private IEnumerable<string> ChunkText(string text, int maxChars = 1500)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;

            // First try split by blank lines (paragraphs)
            var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim())
                                 .Where(p => !string.IsNullOrWhiteSpace(p))
                                 .ToList();

            foreach (var p in paragraphs)
            {
                if (p.Length <= maxChars)
                {
                    yield return p;
                }
                else
                {
                    // further chunk long paragraphs
                    int pos = 0;
                    while (pos < p.Length)
                    {
                        var len = Math.Min(maxChars, p.Length - pos);
                        yield return p.Substring(pos, len);
                        pos += len;
                    }
                }
            }
        }

        public async Task IngestDocumentAsync(string sourceKey, string fullText, CancellationToken ct = default)
        {
            // remove previous chunks for that source
            var existing = _db.DocumentVectors.Where(d => d.Source == sourceKey);
            _db.DocumentVectors.RemoveRange(existing);
            await _db.SaveChangesAsync(ct);

            var chunks = ChunkText(fullText);
            var toAdd = new List<DocumentVector>();
            foreach (var chunk in chunks)
            {
                var vec = await _embed.CreateEmbeddingAsync(chunk, ct);
                var dv = new DocumentVector
                {
                    Id = Guid.NewGuid(),
                    Source = sourceKey,
                    ChunkId = Guid.NewGuid().ToString("N"),
                    Text = chunk,
                    EmbeddingJson = JsonSerializer.Serialize(vec),
                    CreatedAt = DateTime.UtcNow
                };
                toAdd.Add(dv);
            }

            if (toAdd.Count > 0)
            {
                await _db.DocumentVectors.AddRangeAsync(toAdd, ct);
                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Ingested {Count} chunks for source {Source}", toAdd.Count, sourceKey);
        }

        // Retrieve top-k chunks across one or more sources using cosine similarity
        public async Task<List<(DocumentVector Doc, float Score)>> RetrieveAsync(string query, string[] sources, int topK = 3, CancellationToken ct = default)
        {
            var qVec = await _embed.CreateEmbeddingAsync(query, ct);
            var candidates = await _db.DocumentVectors.Where(d => sources.Contains(d.Source)).ToListAsync(ct);
            var results = new List<(DocumentVector, float)>();

            foreach (var c in candidates)
            {
                var vec = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson) ?? Array.Empty<float>();
                var score = CosineSimilarity(qVec, vec);
                results.Add((c, score));
            }

            return results.OrderByDescending(r => r.Item2).Take(topK).ToList();
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0f;
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-12));
        }
    }
}
