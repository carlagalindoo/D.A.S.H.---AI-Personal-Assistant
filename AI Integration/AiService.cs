using Domain.Models;
using System.Text;
using System.Text.Json;


namespace AI_Integration
{
    public class AiService
    {

        private static readonly HttpClient _client = new HttpClient();

        public AiService()
        {
            _client.BaseAddress = new Uri("http://localhost:11434");

        }

        public async Task<ExtractedFacts?> ExtractFactsAsync(string userInput)
        {
            var obj = new {
                model = "qwen3:8b",
                stream = false,
                messages = new[]
                {
                    new {role = "system", content = "extract Who, What, Where, When. Return valid JSON"},
                    new {role = "user", content = userInput}
                }

            };
            using StringContent content = new(
                  JsonSerializer.Serialize(obj),
                  Encoding.UTF8,
                  "application/json"
);

            {
            
                HttpResponseMessage response = await _client.PostAsync("/api/chat", content);

                response.EnsureSuccessStatusCode();

                string vysledek = await response.Content.ReadAsStringAsync();

                var root = JsonDocument.Parse(vysledek);

                string aicontent = root
                     .RootElement
                     .GetProperty("message")
                     .GetProperty("content")
                     .GetString()! ;

                var fakta = JsonSerializer.Deserialize<ExtractedFacts>(
                                                                 aicontent,
                                                 new JsonSerializerOptions
                                                 {
                                                  PropertyNameCaseInsensitive = true
                                                 });
                return fakta;


            }
            ;
          
              



        }

        
    }
}
       