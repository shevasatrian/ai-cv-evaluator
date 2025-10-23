using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ai_cv_evaluator.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _http;
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(IHttpClientFactory factory, IConfiguration config, ILogger<EmbeddingService> logger)
        {
            _http = factory.CreateClient("OpenAI");
            _logger = logger;

            var apiKey = config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("OpenAI:ApiKey");

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            var payload = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("v1/embeddings", content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Embedding API error: {Status} - {Body}", resp.StatusCode, body);
                throw new Exception($"Embedding API failed: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray();
            var list = new List<float>();
            foreach (var v in arr) list.Add(v.GetSingle());
            return list.ToArray();
        }
    }
}
