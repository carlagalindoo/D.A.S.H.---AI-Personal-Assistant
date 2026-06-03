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
          "When must be written as one of: 'today', 'tomorrow', 'in N days', 'in N weeks', 'in N months', " +
          "'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday', " +
          "or a date like 'DD.MM' (e.g. 8.6 for June 8th). " +
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

        public async Task<Domain.Models.UpdateIntent?> ExtractUpdateIntentAsync(
            string userInput,
            IEnumerable<Domain.Models.Task> existingTasks)
        {
            var taskListText = BuildTaskListText(existingTasks);

            var systemPrompt =
                "You are a task management assistant. " +
                "The user wants to update one of their tasks. " +
                "Return ONLY valid JSON in this exact format, nothing else:\n" +
                "{\"TaskId\":-1,\"NewTitle\":\"\",\"NewWhen\":\"\",\"NewWhere\":\"\",\"NewWho\":\"\"}\n" +
                "Rules:\n" +
                "- TaskId must be the integer ID of the task the user is referring to (from the list below). Use -1 if you cannot identify it.\n" +
                "- Only fill in fields the user explicitly wants to CHANGE. Leave others as empty string \"\".\n" +
                "- NewWhen: copy the date/time expression exactly as the user said it (e.g. 'tomorrow at 4pm').\n" +
                "- No explanation. No markdown. No extra text. Only the JSON object.\n\n" +
                "Current tasks:\n" + taskListText;

            var obj = new
            {
                model = _model,
                stream = false,
                messages = new[]
                {
            new { role = "system", content = systemPrompt },
            new { role = "user",   content = $"User request: {userInput}" }
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
                if (!response.IsSuccessStatusCode) return null;

                string result = await response.Content.ReadAsStringAsync();
                using var root = JsonDocument.Parse(result);
                string aiContent = root.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";

                int start = aiContent.IndexOf("{");
                int end = aiContent.LastIndexOf("}");
                if (start == -1 || end == -1 || end <= start) return null;

                return JsonSerializer.Deserialize<Domain.Models.UpdateIntent>(
                    aiContent.Substring(start, end - start + 1),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private static string BuildTaskListText(IEnumerable<Domain.Models.Task> tasks)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in tasks)
                sb.AppendLine($"ID={t.TaskId} | Title=\"{t.Title}\" | Date={t.Date:yyyy-MM-dd} | Time={t.Time:hh\\:mm} | Location=\"{t.Location}\" | People=\"{t.People}\"");
            return sb.Length > 0 ? sb.ToString() : "(no tasks yet)";
        }
    }
}