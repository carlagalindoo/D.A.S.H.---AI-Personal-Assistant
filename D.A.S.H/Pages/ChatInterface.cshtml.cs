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

            AddUserMessage(currentInput);

            ExtractedFacts? facts = null;

            try
            {
                facts = await _aiService.ExtractFactsAsync(currentInput);
            }
            catch
            {
                facts = null;
            }

            var action = facts?.Action?.ToLower();

            // 1. Ollama decides first
            if (action == "read")
            {
                await HandleReadAsync(currentInput);
            }
            else if (action == "update")
            {
                await HandleUpdateAsync(currentInput);
            }
            else if (action == "delete")
            {
                await HandleDeleteAsync(currentInput);
            }
            else if (action == "create")
            {
                await HandleCreateAsync(currentInput);
            }

            // 2. Fallback only if Ollama action is unclear
            else if (LooksLikeUpdateRequest(currentInput))
            {
                await HandleUpdateAsync(currentInput);
            }
            else if (LooksLikeReadRequest(currentInput))
            {
                await HandleReadAsync(currentInput);
            }
            else if (LooksLikeDeleteRequest(currentInput))
            {
                await HandleDeleteAsync(currentInput);
            }
            else
            {
                await HandleCreateAsync(currentInput);
            }

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
                    Time = ParseTime(GetValueOrFallback(facts?.When, input)).TimeOfDay, // <-- FIXED LINE
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
            var facts = await _aiService.ExtractFactsAsync(input);
            var tasks = await _taskRepository.GetAllAsync();

            string searchText = "";

            // 1. Ollama first
            if (!string.IsNullOrWhiteSpace(facts?.What))
            {
                searchText = facts.What;
            }
            else if (!string.IsNullOrWhiteSpace(facts?.Who))
            {
                searchText = facts.Who;
            }

            // 2. Fallback if Ollama gives nothing useful
            if (string.IsNullOrWhiteSpace(searchText) ||
                searchText == "*" ||
                searchText.ToLower() == "unknown" ||
                searchText.ToLower() == "not specified")
            {
                searchText = input
                    .Replace("delete", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("remove", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("task", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("with", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("the", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("?", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
            }

            var taskToDelete = tasks.FirstOrDefault(t =>
                t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.People.Contains(searchText, StringComparison.OrdinalIgnoreCase)
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

        private async System.Threading.Tasks.Task HandleUpdateAsync(string input)
        {
            var facts = await _aiService.ExtractFactsAsync(input);
            var tasks = await _taskRepository.GetAllAsync();

            // Map facts?.TargetTask to facts?.What or rely on extraction
            var targetTask = GetValueOrFallback(
                facts?.What,
                ExtractUpdateTarget(input)
            );

            // Since ExtractedFacts doesn't have dedicated 'new' properties, we rely on the manual extraction methods
            var newTitle = GetValueOrFallback(null, ExtractUpdateNewValue(input));
            var newWhen = GetValueOrFallback(facts?.When, newTitle); // Fallback to newTitle incase AI missed 'When'
            var newWhere = GetValueOrFallback(facts?.Where, "");
            var newWho = GetValueOrFallback(facts?.Who, "");

            var taskToUpdate = tasks.FirstOrDefault(t =>
                t.Title.Contains(targetTask, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(targetTask, StringComparison.OrdinalIgnoreCase)
            );

            if (taskToUpdate == null)
            {
                AiResponse = "I could not find a matching task to update.";
                AddAiMessage("I could not find a matching task to update.", IntentType.Create);
                return;
            }

            // Only update Title if it doesn't strictly look like just a bare time or date adjustment
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                bool isJustTimeOrDate = Regex.IsMatch(newTitle, @"^(\d{1,2})(?::(\d{2}))?\s*(am|pm)$", RegexOptions.IgnoreCase) || 
                                        newTitle.Equals("tomorrow", StringComparison.OrdinalIgnoreCase) || 
                                        newTitle.Equals("today", StringComparison.OrdinalIgnoreCase);

                if (!isJustTimeOrDate)
                {
                    taskToUpdate.Title = ExtractTitle(newTitle);
                    taskToUpdate.Description = input;
                }
            }

            if (!string.IsNullOrWhiteSpace(newWhen))
            {
                // Only update the Date if we explicitly find a supported date expression
                if (newWhen.Contains("tomorrow", StringComparison.OrdinalIgnoreCase) || newWhen.Contains("today", StringComparison.OrdinalIgnoreCase))
                {
                    taskToUpdate.Date = ParseDate(newWhen);
                }

                // Only update the Time if we explicitly find a supported time expression
                if (Regex.IsMatch(newWhen, @"\b(\d{1,2})(?::(\d{2}))?\s*(am|pm)\b", RegexOptions.IgnoreCase))
                {
                    taskToUpdate.Time = ParseTime(newWhen).TimeOfDay;
                }
            }

            if (!string.IsNullOrWhiteSpace(newWhere))
            {
                taskToUpdate.Location = newWhere;
            }

            if (!string.IsNullOrWhiteSpace(newWho))
            {
                taskToUpdate.People = newWho;
            }
            else if (!string.IsNullOrWhiteSpace(newTitle))
            {
                // Extract people safely from newTitle if valid
                var peopleCandidate = ExtractPeople(newTitle);
                if (peopleCandidate != "Not specified")
                {
                    taskToUpdate.People = peopleCandidate;
                }
            }

            await _taskRepository.UpdateAsync(taskToUpdate);

            AiResponse = $"Updated task: {taskToUpdate.Title}";
            AddAiMessage($"Updated task: {taskToUpdate.Title}", IntentType.Create);
        }

        private async System.Threading.Tasks.Task HandleReadAsync(string input)
        {
            var tasks = await _taskRepository.GetAllAsync();

            if (!tasks.Any())
            {
                AiResponse = "There are no tasks yet.";
                AddAiMessage("There are no tasks yet.", IntentType.Create);
                return;
            }

            var searchText = "";

            if (input.ToLower().Contains("with "))
            {
                searchText = input.Split("with").Last().Trim().Replace("?", "");
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                tasks = tasks
                    .Where(t =>
                        t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        t.People.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!tasks.Any())
            {
                AiResponse = "I could not find matching tasks.";
                AddAiMessage("I could not find matching tasks.", IntentType.Create);
                return;
            }

            var response = string.IsNullOrWhiteSpace(searchText)
                ? "Current tasks: " + string.Join(", ", tasks.Select(t => t.Title))
                : $"Tasks with {searchText}: " + string.Join(", ", tasks.Select(t => t.Title));

            AiResponse = response;
            AddAiMessage(response, IntentType.Create);
        }

        private string GetValueOrFallback(string? aiValue, string fallback)
        {
            if (string.IsNullOrWhiteSpace(aiValue))
                return fallback;

            if (aiValue == "*" || aiValue.ToLower() == "not specified" || aiValue.ToLower() == "unknown")
                return fallback;

            return aiValue.Trim();
        }

        private bool LooksLikeUpdateRequest(string input)
        {
            input = input.ToLower();

            return input.StartsWith("update") ||
                   input.StartsWith("change") ||
                   input.Contains("update") ||
                   input.Contains("change");
        }

        private bool LooksLikeReadRequest(string input)
        {
            input = input.ToLower();

            return input.Contains("do i have") ||
                   input.Contains("any tasks") ||
                   input.Contains("show") ||
                   input.Contains("list") ||
                   input.Contains("read") ||
                   input.Contains("tasks with");
        }

        private bool LooksLikeDeleteRequest(string input)
        {
            input = input.ToLower();

            return input.StartsWith("delete") ||
                   input.StartsWith("remove");
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

        private DateTime ParseTime(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DateTime.Today.AddHours(9);

            input = input.ToLower();

            // Match strings dynamically (e.g. "8", "8am", "8:30 pm", "14:00") regardless of "at "
            var match = Regex.Match(input, @"\b(\d{1,2})(?::(\d{2}))?\s*(am|pm)\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int hour = int.Parse(match.Groups[1].Value);
                int minute = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                string ampm = match.Groups[3].Value;

                if (ampm == "pm" && hour < 12)
                    hour += 12;

                if (ampm == "am" && hour == 12)
                    hour = 0;

                if (hour >= 0 && hour <= 23)
                    return DateTime.Today.AddHours(hour).AddMinutes(minute);
            }

            // Fallback to 9 AM if no valid time was found
            return DateTime.Today.AddHours(9);
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

        private string ExtractUpdateTarget(string input)
        {
            var text = input
                .Replace("update", "", StringComparison.OrdinalIgnoreCase)
                .Replace("change", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (text.Contains(" to ", StringComparison.OrdinalIgnoreCase))
            {
                return text.Split(" to ", StringSplitOptions.None)[0].Trim();
            }

            return text;
        }

        private string ExtractUpdateNewValue(string input)
        {
            if (input.Contains(" to ", StringComparison.OrdinalIgnoreCase))
            {
                return input.Split(" to ", StringSplitOptions.None).Last().Trim();
            }

            return "";
        }
    }
}
