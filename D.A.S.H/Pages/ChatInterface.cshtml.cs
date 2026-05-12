using AI_Integration;
using DAL.Repositories;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace D.A.S.H.Pages
{
    public class ChatInterfaceModel : PageModel
    {
        private readonly IAiService _aiService;
        private readonly TaskRepository _taskRepository;

        public ChatInterfaceModel(IAiService aiService, TaskRepository taskRepository)
        {
            _aiService = aiService;
            _taskRepository = taskRepository;
        }

        [BindProperty]
        public string UserRequest { get; set; } = string.Empty;

        public string AiResponse { get; set; } = string.Empty;

        public async System.Threading.Tasks.Task OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UserRequest))
            {
                AiResponse = "Please type something.";
                return;
            }

            try
            {
                var facts = await _aiService.ExtractFactsAsync(UserRequest);

                if (facts == null)
                {
                    var fallbackTask = new Domain.Models.Task
                    {
                        Title = ExtractTitle(UserRequest),
                        Description = UserRequest,
                        Date = ParseDate(UserRequest),
                        Time = ParseTime(UserRequest),
                        Location = ExtractLocation(UserRequest),
                        People = ExtractPeople(UserRequest),
                        SessionKey = "1"
                    };

                    await _taskRepository.AddAsync(fallbackTask);
                    AiResponse = "AI could not extract details, but the task was still created.";

                    return;
                }

                var task = new Domain.Models.Task
                {
                    Title = ExtractTitle(UserRequest),
                    Description = UserRequest,
                    Date = ParseDate(UserRequest),
                    Time = ParseTime(UserRequest),
                    Location = ExtractLocation(UserRequest),
                    People = ExtractPeople(UserRequest),
                    SessionKey = "1"
                };

                await _taskRepository.AddAsync(task);

                AiResponse = "Task created successfully!";
            }
            catch (Exception ex)
            {
                AiResponse = $"AI failed: {ex.Message}";
            }
        }


        private DateTime ParseDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DateTime.Today;

            input = input.ToLower();

            if (input.Contains("tomorrow"))
                return DateTime.Today.AddDays(1);

            if (input.Contains("today"))
                return DateTime.Today;

            return DateTime.Today;
        }

        private DateTime ParseTime(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DateTime.Today.AddHours(9);

            var match = Regex.Match(
                input,
                @"\bat\s+(\d{1,2})(?::(\d{2}))?\s*(am|pm)\b",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return DateTime.Today.AddHours(9);

            int hour = int.Parse(match.Groups[1].Value);
            int minute = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            string ampm = match.Groups[3].Value.ToLower();

            if (ampm == "pm" && hour < 12)
                hour += 12;

            if (ampm == "am" && hour == 12)
                hour = 0;

            return DateTime.Today.AddHours(hour).AddMinutes(minute);
        }

        private string ExtractLocation(string input)
        {
            string lower = input.ToLower();

            if (lower.Contains(" in "))
            {
                var location = input.Split(" in ").LastOrDefault();
                return CleanLocation(location);
            }

            if (lower.Contains(" at "))
            {
                var afterAt = input.Split(" at ").LastOrDefault();

                // If "at" is followed by time, don't treat it as location
                if (afterAt != null &&
                    (afterAt.Contains("AM", StringComparison.OrdinalIgnoreCase) ||
                     afterAt.Contains("PM", StringComparison.OrdinalIgnoreCase) ||
                     char.IsDigit(afterAt.Trim()[0])))
                {
                    return "Not specified";
                }

                return CleanLocation(afterAt);
            }

            return "Not specified";
        }

        private string CleanLocation(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return "Not specified";

            location = location.Trim();

            // remove time words from location
            location = location.Replace("tomorrow", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("today", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("at 8", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("at 3 pm", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("3 pm", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("8 pm", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("8 am", "", StringComparison.OrdinalIgnoreCase);

            return string.IsNullOrWhiteSpace(location)
                ? "Not specified"
                : location.Trim();
        }

        private string ExtractPeople(string input)
        {
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[] verbs =
            {
                "walk",
                "feed",
                "take",
                "bring",
                "call",
                "meet"
            };

            for (int i = 0; i < words.Length - 1; i++)
            {
                if (verbs.Contains(words[i].ToLower()))
                {
                    string nextWord = words[i + 1];

                    // ignore time/location keywords
                    if (!nextWord.Equals("tomorrow", StringComparison.OrdinalIgnoreCase) &&
                        !nextWord.Equals("today", StringComparison.OrdinalIgnoreCase) &&
                        !nextWord.Equals("at", StringComparison.OrdinalIgnoreCase) &&
                        !nextWord.Equals("in", StringComparison.OrdinalIgnoreCase))
                    {
                        return nextWord;
                    }
                }
            }

            // still support "with Sarah"
            if (input.ToLower().Contains(" with "))
            {
                var person = input.Split(" with ").LastOrDefault();
                return CleanPeople(person);
            }

            return "Not specified";
        }

        private string CleanPeople(string? person)
        {
            if (string.IsNullOrWhiteSpace(person))
                return "Not specified";

            person = person.Trim();

            if (person.Contains(" at "))
                person = person.Split(" at ")[0];

            if (person.Contains(" in "))
                person = person.Split(" in ")[0];

            return string.IsNullOrWhiteSpace(person)
                ? "Not specified"
                : person.Trim();
        }

        private string ExtractTitle(string input)
        {
            var title = input;

            if (title.Contains(" tomorrow", StringComparison.OrdinalIgnoreCase))
                title = title.Split(" tomorrow", StringSplitOptions.None)[0];

            if (title.Contains(" today", StringComparison.OrdinalIgnoreCase))
                title = title.Split(" today", StringSplitOptions.None)[0];

            if (title.Contains(" at ", StringComparison.OrdinalIgnoreCase))
                title = title.Split(" at ", StringSplitOptions.None)[0];

            if (title.Contains(" with ", StringComparison.OrdinalIgnoreCase))
                title = title.Split(" with ", StringSplitOptions.None)[0];

            if (title.Contains(" in ", StringComparison.OrdinalIgnoreCase))
                title = title.Split(" in ", StringSplitOptions.None)[0];

            return string.IsNullOrWhiteSpace(title)
                ? input
                : title.Trim();
        }
    }
}
