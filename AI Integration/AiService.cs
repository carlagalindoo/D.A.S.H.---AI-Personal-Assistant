using Domain.Models;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
                model = "mistral",
                stream = false,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"You are a task assistant.

                        Return ONLY valid JSON.
                        No explanation.
                        No markdown.

                        Action must be one of:
                        Create, Read, Update, Delete.

                        For CREATE, use:
                        {
                            ""Action"": ""Create"",
                            ""Who"": """",
                            ""What"": """",
                            ""Where"": """",
                            ""When"": """",
                            ""TargetTask"": """",
                            ""NewTitle"": """",
                            ""NewWhen"": """",
                            ""NewWhere"": """",
                            ""NewWho"": """"
                        }

                        For READ, use:
                        {
                            ""Action"": ""Read"",
                            ""Who"": """",
                            ""What"": """",
                            ""Where"": """",
                            ""When"": """",
                            ""TargetTask"": """",
                            ""NewTitle"": """",
                            ""NewWhen"": """",
                            ""NewWhere"": """",
                            ""NewWho"": """"
                        }

                        For DELETE, use:
                        {
                            ""Action"": ""Delete"",
                            ""Who"": """",
                            ""What"": """",
                            ""Where"": """",
                            ""When"": """",
                            ""TargetTask"": """",
                            ""NewTitle"": """",
                            ""NewWhen"": """",
                            ""NewWhere"": """",
                            ""NewWho"": """"
                        }

                        For update requests:
                        - TargetTask is the existing task to find.
                        - NewTitle is the new title only if the title changes.
                        - NewWhen is the new date/time only if date or time changes.
                        - NewWhere is the new location only if location changes.
                        - NewWho is the new person only if person changes.

                        Example:
                        User: update Walk Bella to 6 PM

                        JSON:
                        {
                            ""Action"": ""Update"",
                            ""Who"": """",
                            ""What"": """",
                            ""Where"": """",
                            ""When"": """",
                            ""TargetTask"": ""Walk Bella"",
                            ""NewTitle"": """",
                            ""NewWhen"": ""6 PM"",
                            ""NewWhere"": """",
                            ""NewWho"": """"
                        }"

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