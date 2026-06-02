using Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AI_Integration
{
    public class AiService : IAiService
    {
        private readonly HttpClient _client;
        private readonly string _model;

        public AiService(IConfiguration configuration)
        {
            var baseUrl = configuration["AI:BaseUrl"] ?? "http://localhost:11434/";
            _model = configuration["AI:Model"] ?? "phi3:mini";

            _client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task<ExtractedFacts?> ExtractFactsAsync(string userInput)
        {
            var obj = new
            {
                model = _model,
                stream = false,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Return ONLY valid JSON in this exact format, nothing else: " +
                                  "{\"Action\":\"\",\"Who\":\"\",\"What\":\"\",\"Where\":\"\",\"When\":\"\"}. " +
                                  "Action must be exactly one of: Create, Read, Update, Delete. " +
                                  "If a field is unknown, use an empty string. No explanation. No markdown. No extra text."
                    },
                    new
                    {
                        role = "user",
                        content = $"Text: {userInput}"
                    }
                }
            };

            using StringContent content = new(
                JsonSerializer.Serialize(obj),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                HttpResponseMessage response = await _client.PostAsync("api/chat", content);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string result = await response.Content.ReadAsStringAsync();

                using var root = JsonDocument.Parse(result);

                string aiContent = root
                    .RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                int start = aiContent.IndexOf("{");
                int end = aiContent.LastIndexOf("}");

                if (start == -1 || end == -1 || end <= start)
                {
                    return null;
                }

                string jsonOnly = aiContent.Substring(start, end - start + 1);

                return JsonSerializer.Deserialize<ExtractedFacts>(
                    jsonOnly,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"AI service error: {ex.Message}");
            }
        }
    }
}