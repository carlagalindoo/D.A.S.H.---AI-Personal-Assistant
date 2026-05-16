using Domain.Models;
using System.Text;
using System.Text.Json;

namespace AI_Integration
{
    public class AiService : IAiService
    {
        private static readonly HttpClient _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };

        public AiService()
        {
        }

        public async Task<ExtractedFacts?> ExtractFactsAsync(string userInput)
        {
            var obj = new
            {
                model = "tinyllama",
                stream = false,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Return ONLY JSON in this exact format: {\"Action\":\"\",\"Who\":\"\",\"What\":\"\",\"Where\":\"\",\"When\":\"\"}. Action must be one of: Create, Read, Update, Delete. No explanation. No markdown."
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
            catch
            {
                return null;
            }
        }
    }
}