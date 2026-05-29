using AI_Integration;
using DAL.Repositories;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
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

        public List<Message> ChatMessages { get; set; } = new();

        public void OnGet()
        {
            LoadChatMessages();
        }

        public async System.Threading.Tasks.Task OnPostAsync()
        {
            LoadChatMessages();

            if (string.IsNullOrWhiteSpace(UserRequest))
            {
                AddAiMessage("Please type something.", IntentType.Create);
                SaveChatMessages();
                return;
            }

            var currentInput = UserRequest.Trim();
            var lowerInput = currentInput.ToLower();

            AddUserMessage(currentInput);

            bool isDeleteRequest =
                lowerInput.StartsWith("delete") ||
                lowerInput.StartsWith("remove");

            if (isDeleteRequest)
            {
                await HandleDeleteAsync(currentInput);
                SaveChatMessages();
                UserRequest = string.Empty;
                return;
            }

            bool isReadRequest =
                lowerInput.Contains("show") ||
                lowerInput.Contains("list") ||
                lowerInput.Contains("read");

            if (isReadRequest)
            {
                await HandleReadAsync();
                SaveChatMessages();
                UserRequest = string.Empty;
                return;
            }

            bool isUpdateRequest =
            lowerInput.StartsWith("update") ||
            lowerInput.StartsWith("change") ||
            lowerInput.StartsWith("edit");

            if (isUpdateRequest)
            {
                await HandleUpdateAsync(currentInput);
                SaveChatMessages();
                UserRequest = string.Empty;
                return;
            }

            await HandleCreateAsync(currentInput);

            SaveChatMessages();
            UserRequest = string.Empty;
        }

        private async System.Threading.Tasks.Task HandleCreateAsync(string input)
        {
            try
            {
                var facts = await _aiService.ExtractFactsAsync(input);

                var task = new Domain.Models.Task
                {
                    Title = GetValueOrFallback(facts?.What, ExtractTitle(input)),
                    Description = input,
                    Date = ParseDate(GetValueOrFallback(facts?.When, input)),
                    Time = ParseTime(GetValueOrFallback(facts?.When, input)),
                    Location = GetValueOrFallback(facts?.Where, ExtractLocation(input)),
                    People = GetValueOrFallback(facts?.Who, ExtractPeople(input)),
                    SessionKey = "1"
                };

                await _taskRepository.AddAsync(task);

                AiResponse = "Task created successfully!";
                AddAiMessage("Task created successfully!", IntentType.Create);
            }
            catch (Exception ex)
            {
                AiResponse = $"AI failed: {ex.Message}";
                AddAiMessage($"AI failed: {ex.Message}", IntentType.Create);
            }
        }

        private async System.Threading.Tasks.Task HandleDeleteAsync(string input)
        {
            var tasks = await _taskRepository.GetAllAsync();

            var searchText = input
                .Replace("delete", "", StringComparison.OrdinalIgnoreCase)
                .Replace("remove", "", StringComparison.OrdinalIgnoreCase)
                .Replace("task", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            var taskToDelete = tasks.FirstOrDefault(t =>
                t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            );

            if (taskToDelete == null)
            {
                AiResponse = "I could not find a matching task to delete.";
                AddAiMessage("I could not find a matching task to delete.", IntentType.Delete);
                return;
            }

            await _taskRepository.DeleteAsync(taskToDelete.TaskId);

            AiResponse = $"Deleted task: {taskToDelete.Title}";
            AddAiMessage($"Deleted task: {taskToDelete.Title}", IntentType.Delete);
        }

        private async System.Threading.Tasks.Task HandleReadAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();

            if (!tasks.Any())
            {
                AiResponse = "There are no tasks yet.";
                AddAiMessage("There are no tasks yet.", IntentType.Create);
                return;
            }

            var response = "Current tasks: " + string.Join(", ", tasks.Select(t => t.Title));

            AiResponse = response;
            AddAiMessage(response, IntentType.Create);
        }

        private async System.Threading.Tasks.Task HandleUpdateAsync(string input)
        {
            var tasks = await _taskRepository.GetAllAsync();

            // Strip the command word, then try to split "old value" from "new value"
            // e.g. "update meeting with John to tomorrow at 4pm"
            var stripped = input
                .Replace("update", "", StringComparison.OrdinalIgnoreCase)
                .Replace("change", "", StringComparison.OrdinalIgnoreCase)
                .Replace("edit", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            // Try to find the task by matching the first part of the input
            // Split on " to " so "meeting to tomorrow at 4pm" → search for "meeting"
            string searchText = stripped.Contains(" to ", StringComparison.OrdinalIgnoreCase)
                ? stripped.Split(" to ", StringSplitOptions.None)[0].Trim()
                : stripped;

            var taskToUpdate = tasks.FirstOrDefault(t =>
                t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            );

            if (taskToUpdate == null)
            {
                AddAiMessage($"I could not find a task matching '{searchText}'.", IntentType.Update);
                return;
            }

            // Send the FULL original input to the AI so it can extract the new values
            var facts = await _aiService.ExtractFactsAsync(input);

            bool updated = false;

            if (!string.IsNullOrWhiteSpace(facts?.What) &&
                facts.What != "*" &&
                facts.What.ToLower() != "not specified")
            {
                taskToUpdate.Title = facts.What;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(facts?.When) &&
                facts.When != "*" &&
                facts.When.ToLower() != "not specified")
            {
                taskToUpdate.Date = ParseDate(facts.When);
                taskToUpdate.Time = ParseTime(facts.When);
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(facts?.Where) &&
                facts.Where != "*" &&
                facts.Where.ToLower() != "not specified")
            {
                taskToUpdate.Location = facts.Where;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(facts?.Who) &&
                facts.Who != "*" &&
                facts.Who.ToLower() != "not specified")
            {
                taskToUpdate.People = facts.Who;
                updated = true;
            }

            if (!updated)
            {
                AddAiMessage($"I found '{taskToUpdate.Title}' but couldn't understand what to change. Try something like: 'update meeting to tomorrow at 4pm'.", IntentType.Update);
                return;
            }

            await _taskRepository.UpdateAsync(taskToUpdate);

            var summary = new List<string>();

            if (!string.IsNullOrWhiteSpace(facts?.What) && facts.What != "*" && facts.What.ToLower() != "not specified")
                summary.Add($"title to '{taskToUpdate.Title}'");

            if (!string.IsNullOrWhiteSpace(facts?.When) && facts.When != "*" && facts.When.ToLower() != "not specified")
                summary.Add($"time to {taskToUpdate.Time:hh\\:mm} on {taskToUpdate.Date:dd/MM/yyyy}");

            if (!string.IsNullOrWhiteSpace(facts?.Where) && facts.Where != "*" && facts.Where.ToLower() != "not specified")
                summary.Add($"location to '{taskToUpdate.Location}'");

            if (!string.IsNullOrWhiteSpace(facts?.Who) && facts.Who != "*" && facts.Who.ToLower() != "not specified")
                summary.Add($"people to '{taskToUpdate.People}'");

            AddAiMessage($"Updated '{taskToUpdate.Title}': changed {string.Join(", ", summary)}.", IntentType.Update);
        }

        private string GetValueOrFallback(string? aiValue, string fallback)
        {
            if (string.IsNullOrWhiteSpace(aiValue))
                return fallback;

            if (aiValue == "*" || aiValue.ToLower() == "not specified" || aiValue.ToLower() == "unknown")
                return fallback;

            return aiValue.Trim();
        }

        private void AddUserMessage(string text)
        {
            ChatMessages.Add(new Message
            {
                Text = text,
                SentAt = DateTime.Now,
                Sender = "You",
                Intent = IntentType.Create,
                ChatHistoryId = 1
            });
        }

        private void AddAiMessage(string text, IntentType intent)
        {
            ChatMessages.Add(new Message
            {
                Text = text,
                SentAt = DateTime.Now,
                Sender = "AI",
                Intent = intent,
                ChatHistoryId = 1
            });
        }

        private void LoadChatMessages()
        {
            var json = HttpContext.Session.GetString("ChatMessages");

            ChatMessages = string.IsNullOrEmpty(json)
                ? new List<Message>()
                : JsonSerializer.Deserialize<List<Message>>(json) ?? new List<Message>();
        }

        private void SaveChatMessages()
        {
            HttpContext.Session.SetString("ChatMessages", JsonSerializer.Serialize(ChatMessages));
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

        private TimeSpan ParseTime(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new TimeSpan(9, 0, 0);

            input = input.ToLower();

            if (!input.Contains(" at "))
                return new TimeSpan(9, 0, 0);

            var afterAt = input.Split(" at ").Last().Trim();
            var parts = afterAt.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return new TimeSpan(9, 0, 0);

            string timePart = parts[0];
            string ampm = parts.Length > 1 ? parts[1] : "";

            int hour;
            int minute = 0;

            if (timePart.Contains(":"))
            {
                var timePieces = timePart.Split(":");
                if (!int.TryParse(timePieces[0], out hour))
                    return new TimeSpan(9, 0, 0);
                int.TryParse(timePieces[1], out minute);
            }
            else
            {
                if (!int.TryParse(timePart, out hour))
                    return new TimeSpan(9, 0, 0);
            }

            if (ampm == "pm" && hour < 12) hour += 12;
            if (ampm == "am" && hour == 12) hour = 0;
            if (hour < 0 || hour > 23) return new TimeSpan(9, 0, 0);

            return new TimeSpan(hour, minute, 0);
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

                if (string.IsNullOrWhiteSpace(afterAt))
                    return "Not specified";

                var trimmed = afterAt.Trim();

              
                if (char.IsDigit(trimmed[0]))
                    return "Not specified";

                if (trimmed.StartsWith("am") || trimmed.StartsWith("pm"))
                    return "Not specified";

                return CleanLocation(afterAt);
            }

            if (lower.Contains(" to "))
            {
                var location = input.Split(" to ").LastOrDefault();
                return CleanLocation(location);
            }

            return "Not specified";
        }

        private string CleanLocation(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return "Not specified";

            location = location.Trim();

            location = location.Replace("tomorrow", "", StringComparison.OrdinalIgnoreCase);
            location = location.Replace("today", "", StringComparison.OrdinalIgnoreCase);

            var atIndex = location.IndexOf(" at ", StringComparison.OrdinalIgnoreCase);
            if (atIndex >= 0)
                location = location.Substring(0, atIndex);

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

                    if (!nextWord.Equals("tomorrow", StringComparison.OrdinalIgnoreCase) &&
                        !nextWord.Equals("today", StringComparison.OrdinalIgnoreCase) &&
                        !nextWord.Equals("at", StringComparison.OrdinalIgnoreCase) &&
                        !nextWord.Equals("in", StringComparison.OrdinalIgnoreCase))
                    {
                        return nextWord;
                    }
                }
            }

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


            if (title.Contains(" in ", StringComparison.OrdinalIgnoreCase))
                title = title.Split(" in ", StringSplitOptions.None)[0];

            return string.IsNullOrWhiteSpace(title)
                ? input
                : title.Trim();
        }
    }
}
