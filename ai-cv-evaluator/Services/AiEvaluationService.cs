using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace ai_cv_evaluator.Services
{
    public class AiEvaluationService
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<AiEvaluationService> _logger;
        private readonly RAGService _rag;

        public AiEvaluationService(IConfiguration config, ILogger<AiEvaluationService> logger, RAGService rag)
        {
            var apiKey = config["OpenAI:ApiKey"];
            _client = new OpenAIClient(apiKey);
            _logger = logger;
            _rag = rag;
        }

        public async Task<(double, string)> EvaluateCVAsync(string jobTitle, string cvText, CancellationToken ct = default)
        {
            // Build query and retrieve JD + rubric contexts
            var jdContextChunks = await _rag.RetrieveAsync(jobTitle, new[] { "job_description", "cv_rubric" }, 3, ct);
            var jdContext = string.Join("\n\n---\n\n", jdContextChunks.Select(x => x.Doc.Text));

            var system = $"""
You are an expert technical recruiter evaluating a CV against the Job Description and CV scoring rubric.
Instructions: give JSON with fields: cv_match_rate (0.0-1.0), cv_feedback (string).
Use only the contextual information provided below where relevant.
""";

            var user = $"""
Job Title: {jobTitle}

Job Description / Rubric context:
{jdContext}

Candidate CV:
{cvText}
""";

            var chatClient = _client.GetChatClient("gpt-4o-mini");
            var response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(system),
                    new UserChatMessage(user)
                ],
                new ChatCompletionOptions { Temperature = 0.2f, MaxOutputTokenCount = 800 }
            );

            var raw = response.Value.Content[0].Text;
            _logger.LogInformation("CV evaluate raw: {Raw}", raw);
            var json = ExtractJsonString(raw);
            using var doc = JsonDocument.Parse(json);
            double match = doc.RootElement.GetProperty("cv_match_rate").GetDouble();
            string feedback = doc.RootElement.GetProperty("cv_feedback").GetString() ?? "";
            return (match, feedback);
        }

        public async Task<(double, string)> EvaluateProjectAsync(string jobTitle, string projectText, CancellationToken ct = default)
        {
            var briefContext = await _rag.RetrieveAsync(jobTitle, new[] { "case_study", "project_rubric" }, 3, ct);
            var briefText = string.Join("\n\n---\n\n", briefContext.Select(x => x.Doc.Text));

            var system = $"""
You are an expert evaluating a candidate's project report against the case study brief and project rubric.
Return JSON with: project_score (1.0-5.0), project_feedback (string).
""";

            var user = $"""
Case Study / Rubric:
{briefText}

Project Report:
{projectText}
""";

            var chatClient = _client.GetChatClient("gpt-4o-mini");
            var response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(system),
                    new UserChatMessage(user)
                ],
                new ChatCompletionOptions { Temperature = 0.2f, MaxOutputTokenCount = 800 }
            );

            var raw = response.Value.Content[0].Text;
            _logger.LogInformation("Project evaluate raw: {Raw}", raw);
            var json = ExtractJsonString(raw);
            using var doc = JsonDocument.Parse(json);
            double score = doc.RootElement.GetProperty("project_score").GetDouble();
            string feedback = doc.RootElement.GetProperty("project_feedback").GetString() ?? "";
            return (score, feedback);
        }

        public async Task<EvaluationResultModel> EvaluateAsync(string jobTitle, string cvText, string projectText, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("🚀 Starting full evaluation pipeline for {JobTitle}", jobTitle);

                // 🔹 Step 1: CV Evaluation
                var (cvMatchRate, cvFeedback) = await EvaluateCVAsync(jobTitle, cvText, ct);
                _logger.LogInformation("✅ CV evaluation complete. MatchRate={Match}, Feedback={Feedback}", cvMatchRate, cvFeedback);

                // 🔹 Step 2: Project Evaluation
                var (projectScore, projectFeedback) = await EvaluateProjectAsync(jobTitle, projectText, ct);
                _logger.LogInformation("✅ Project evaluation complete. Score={Score}, Feedback={Feedback}", projectScore, projectFeedback);

                // 🔹 Step 3: Overall Summary
                var overallSummary = await SummarizeOverallAsync((cvMatchRate, cvFeedback), (projectScore, projectFeedback), ct);
                _logger.LogInformation("✅ Overall summary generated.");

                // 🔹 Step 4: Return Combined Result
                return new EvaluationResultModel
                {
                    Cv_Match_Rate = cvMatchRate,
                    Cv_Feedback = cvFeedback,
                    Project_Score = projectScore,
                    Project_Feedback = projectFeedback,
                    Overall_Summary = overallSummary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in full AI evaluation pipeline.");
                return new EvaluationResultModel
                {
                    Cv_Match_Rate = 0,
                    Cv_Feedback = $"AI evaluation failed: {ex.Message}",
                    Project_Score = 1,
                    Project_Feedback = "Evaluation failed.",
                    Overall_Summary = "Pipeline failed due to an exception."
                };
            }
        }


        public async Task<string> SummarizeOverallAsync((double, string) cvResult, (double, string) projectResult, CancellationToken ct = default)
        {
            var system = "You are an expert synthesizer. Create a 2-3 sentence overall_summary based on CV and Project results. Output plain text.";
            var user = $"""
CV result: match_rate={cvResult.Item1}, feedback={cvResult.Item2}

Project result: score={projectResult.Item1}, feedback={projectResult.Item2}
""";
            var chatClient = _client.GetChatClient("gpt-4o-mini");
            var response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(system),
                    new UserChatMessage(user)
                ],
                new ChatCompletionOptions { Temperature = 0.1f, MaxOutputTokenCount = 300 }
            );

            var raw = response.Value.Content[0].Text;
            return raw?.Trim() ?? "";
        }

        private static string ExtractJsonString(string text)
        {
            int s = text.IndexOf('{');
            int e = text.LastIndexOf('}');
            if (s >= 0 && e > s) return text.Substring(s, e - s + 1);
            return text;
        }
    }

    public class EvaluationResultModel
    {
        public double Cv_Match_Rate { get; set; }
        public string Cv_Feedback { get; set; } = "";
        public double Project_Score { get; set; }
        public string Project_Feedback { get; set; } = "";
        public string Overall_Summary { get; set; } = "";
    }
}
