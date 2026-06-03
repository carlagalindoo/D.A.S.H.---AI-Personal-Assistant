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
            var lowerInput = currentInput.ToLower(); // FIX: Added missing lowerInput variable

            AddUserMessage(currentInput);

            bool isDeleteRequest =
                lowerInput.StartsWith("delete") ||
                lowerInput.StartsWith("remove") ||
                lowerInput.StartsWith("erase") ||
                lowerInput.StartsWith("cancel") ||
                lowerInput.StartsWith("get rid of");

            ExtractedFacts? facts = null; // FIX: Ensure facts is strongly typed and available

            try
            {
                facts = await _aiService.ExtractFactsAsync(currentInput);
            }
            catch
            {
                facts = null;
            }

            string? action = facts?.Action?.ToLower(); // FIX: Added mapping for action variable

            bool isReadRequest =
                lowerInput.Contains("show") ||
                lowerInput.Contains("list") ||
                lowerInput.Contains("read") ||
                lowerInput.Contains("view") ||
                lowerInput.Contains("display") ||
                lowerInput.Contains("see");

            // 1. Ollama decides first
            if (action == "read")
            {
                await HandleReadAsync(currentInput, facts);
                SaveChatMessages();
                UserRequest = string.Empty;
                return;
            }

            bool isUpdateRequest =
                lowerInput.StartsWith("update") ||
                lowerInput.StartsWith("change") ||
                lowerInput.StartsWith("edit") ||
                lowerInput.StartsWith("modify") ||
                lowerInput.StartsWith("rename") ||
                lowerInput.StartsWith("reschedule");

            if (isUpdateRequest || action == "update") // FIX: Also check fallback action
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
                await HandleReadAsync(currentInput, facts);
            }
            else if (LooksLikeDeleteRequest(currentInput) || isDeleteRequest)
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
                AiResponse = $"Something went wrong while creating your task. Please try again.";
                AddAiMessage($"Something went wrong while creating your task. Please try again.", IntentType.Create);
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

            var matchingTasks = tasks.Where(t =>
                t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (!matchingTasks.Any())
            {
                AiResponse = "I could not find a matching task to delete.";
                AddAiMessage("I could not find a matching task to delete.", IntentType.Delete);
                return;
            }

            if (matchingTasks.Count > 1)
            {
                var titles = string.Join(", ", matchingTasks.Select(t => $"'{t.Title}'"));
                AddAiMessage($"I found multiple tasks matching '{searchText}': {titles}. Please be more specific.", IntentType.Delete);
                return;
            }

            var taskToDelete = matchingTasks.First();

            await _taskRepository.DeleteAsync(taskToDelete.TaskId);

            AiResponse = $"Deleted task: {taskToDelete.Title}";
            AddAiMessage($"Deleted task: {taskToDelete.Title}", IntentType.Delete);
        }

        private async System.Threading.Tasks.Task HandleReadAsync(string input, ExtractedFacts? facts)
        {
            var tasks = await _taskRepository.GetAllAsync();

            if (!tasks.Any())
            {
                AiResponse = "There are no tasks yet.";
                AddAiMessage("There are no tasks yet.", IntentType.Query);
                return;
            }

            var filteredTasks = tasks.AsEnumerable();
            bool hasFilters = false;

            // 1. Identify filters extracted by the AI
            if (facts != null)
            {
                if (!string.IsNullOrWhiteSpace(facts.Who) && facts.Who != "*" && facts.Who.ToLower() != "not specified")
                {
                    filteredTasks = filteredTasks.Where(t => t.People != null && t.People.Contains(facts.Who, StringComparison.OrdinalIgnoreCase));
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(facts.Where) && facts.Where != "*" && facts.Where.ToLower() != "not specified")
                {
                    filteredTasks = filteredTasks.Where(t => t.Location != null && t.Location.Contains(facts.Where, StringComparison.OrdinalIgnoreCase));
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(facts.What) && facts.What != "*" && facts.What.ToLower() != "not specified")
                {
                    // Ignore overly generic terms the AI sometimes hallucinates like just "task" or "tasks"
                    if (facts.What.ToLower() != "task" && facts.What.ToLower() != "tasks")
                    {
                        filteredTasks = filteredTasks.Where(t =>
                            (t.Title != null && t.Title.Contains(facts.What, StringComparison.OrdinalIgnoreCase)) ||
                            (t.Description != null && t.Description.Contains(facts.What, StringComparison.OrdinalIgnoreCase)));
                        hasFilters = true;
                    }
                }
            }

            // 2. Safeguard: Directly parse from user input to never miss explicit date/time criteria
            if (input.Contains("today", StringComparison.OrdinalIgnoreCase) || input.Contains("tomorrow", StringComparison.OrdinalIgnoreCase))
            {
                var parsedDate = ParseDate(input);
                filteredTasks = filteredTasks.Where(t => t.Date.Date == parsedDate.Date);
                hasFilters = true;
            }

            var timeMatch = Regex.Match(input, @"\b(\d{1,2})(?::(\d{2}))?\s*(am|pm)\b", RegexOptions.IgnoreCase);
            if (timeMatch.Success)
            {
                var parsedTime = ParseTime(input).TimeOfDay;
                filteredTasks = filteredTasks.Where(t => t.Time == parsedTime);
                hasFilters = true;
            }

            var matchingTasks = hasFilters ? filteredTasks.ToList() : new List<Domain.Models.Task>();

            // 3. Fallback text query parsing ONLY if absolutely no filters were found to prevent accidental query mismatches
            if (!hasFilters)
            {
                var searchText = input
                    .Replace("show me", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("show", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("view", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("display", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("see", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("list", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("read", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("details of", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("details for", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("details about", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("details", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("all the tasks", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("all tasks", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("the task", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("tasks", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("task", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (!string.IsNullOrWhiteSpace(searchText) && searchText.Length >= 2)
                {
                    matchingTasks = tasks.Where(t =>
                        (t.Title != null && t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Description != null && t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    if (matchingTasks.Any())
                    {
                        hasFilters = true;
                    }
                }
            }

            // 4. Returning matched results safely - Now only showing the Task Title
            if (matchingTasks.Any())
            {
                var response = "Here are the tasks I found:\n" + string.Join("\n", matchingTasks.Select(t => $"- {t.Title}"));

                AiResponse = response;
                AddAiMessage(response, IntentType.Query);
                return;
            }

            if (hasFilters)
            {
                // This branch prevents throwing "all tasks" at the user when their filter simply returned 0 results. 
                var failureMsg = "No tasks found matching your criteria.";
                AiResponse = failureMsg;
                AddAiMessage(failureMsg, IntentType.Query);
                return;
            }

            var allTasks = "Current tasks: " + string.Join(", ", tasks.Select(t => t.Title));
            AiResponse = allTasks;
            AddAiMessage(allTasks, IntentType.Query);
        }

        private async System.Threading.Tasks.Task HandleUpdateAsync(string input)
        {
            var tasks = (await _taskRepository.GetAllAsync()).ToList();

            if (!tasks.Any())
            {
                AddAiMessage("You have no tasks to update.", IntentType.Update);
                return;
            }

            // Step 1: Ask the AI — now with the full task list as context
            Domain.Models.Task? taskToUpdate = null;
            Domain.Models.UpdateIntent? intent = null;

            try
            {
                intent = await _aiService.ExtractUpdateIntentAsync(input, tasks);
            }
            catch { intent = null; }

            if (intent != null && intent.TaskId > 0)
                taskToUpdate = tasks.FirstOrDefault(t => t.TaskId == intent.TaskId);

            // Step 2: Fallback — scored keyword matching if AI couldn't identify the task
            if (taskToUpdate == null)
            {
                var stripped = input
                    .Replace("update", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("change", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("edit", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("modify", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("reschedule", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                string searchText = stripped.Contains(" to ", StringComparison.OrdinalIgnoreCase)
                    ? stripped.Split(" to ", StringSplitOptions.None)[0].Trim()
                    : stripped;

                var scored = tasks
                    .Select(t => new { Task = t, Score = ScoreMatch(t, searchText) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ToList();

                if (scored.Count == 0)
                {
                    AddAiMessage(
                        $"I could not find a task matching your request. " +
                        $"Your tasks are: {string.Join(", ", tasks.Select(t => $"'{t.Title}'"))}.",
                        IntentType.Update);
                    return;
                }

                if (scored.Count > 1 && scored[0].Score == scored[1].Score)
                {
                    var titles = string.Join(", ", scored.Take(5).Select(x => $"'{x.Task.Title}'"));
                    AddAiMessage($"I found multiple tasks that could match: {titles}. Please be more specific.", IntentType.Update);
                    return;
                }

                taskToUpdate = scored[0].Task;
            }

            // Step 3: Apply changes
            bool updated = false;
            var summary = new List<string>();

            bool HasValue(string? v) =>
                !string.IsNullOrWhiteSpace(v) &&
                v != "*" &&
                !v.Equals("not specified", StringComparison.OrdinalIgnoreCase) &&
                !v.Equals("unknown", StringComparison.OrdinalIgnoreCase);

            if (HasValue(intent?.NewTitle))
            {
                taskToUpdate.Title = intent!.NewTitle!.Trim();
                summary.Add($"title to '{taskToUpdate.Title}'");
                updated = true;
            }

            if (HasValue(intent?.NewWhen))
            {
                taskToUpdate.Date = ParseDate(intent!.NewWhen);
                taskToUpdate.Time = ParseTime(intent.NewWhen).TimeOfDay;
                summary.Add($"date/time to {taskToUpdate.Date:dd/MM/yyyy} {taskToUpdate.Time:hh\\:mm}");
                updated = true;
            }
            else
            {
                // Even without AI, parse date/time directly from the raw input
                var parsedWhen = TryExtractWhenFromInput(input);
                if (parsedWhen.HasValue)
                {
                    taskToUpdate.Date = parsedWhen.Value.Date;
                    taskToUpdate.Time = parsedWhen.Value.TimeOfDay;
                    summary.Add($"date/time to {taskToUpdate.Date:dd/MM/yyyy} {taskToUpdate.Time:hh\\:mm}");
                    updated = true;
                }
            }

            if (HasValue(intent?.NewWhere))
            {
                taskToUpdate.Location = intent!.NewWhere!.Trim();
                summary.Add($"location to '{taskToUpdate.Location}'");
                updated = true;
            }

            if (HasValue(intent?.NewWho))
            {
                taskToUpdate.People = intent!.NewWho!.Trim();
                summary.Add($"people to '{taskToUpdate.People}'");
                updated = true;
            }

            if (!updated)
            {
                AddAiMessage(
                    $"I found the task '{taskToUpdate.Title}' but couldn't understand what to change. " +
                    $"Try: 'update {taskToUpdate.Title} to tomorrow at 4pm' or 'change {taskToUpdate.Title} location to Room B'.",
                    IntentType.Update);
                return;
            }

            await _taskRepository.UpdateAsync(taskToUpdate);
            AddAiMessage($"Updated '{taskToUpdate.Title}': changed {string.Join(", ", summary)}.", IntentType.Update);
        }

        private static int ScoreMatch(Domain.Models.Task task, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return 0;
            if (task.Title.Equals(searchText, StringComparison.OrdinalIgnoreCase)) return 100;
            if (task.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) return 70;
            if (task.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) return 50;

            var searchWords = searchText
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            if (searchWords.Count == 0) return 0;

            int hits = searchWords.Count(w =>
                task.Title.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                task.Description?.Contains(w, StringComparison.OrdinalIgnoreCase) == true);

            return hits >= Math.Ceiling(searchWords.Count / 2.0) ? hits * 10 : 0;
        }

        private DateTime? TryExtractWhenFromInput(string input)
        {
            bool hasTime = System.Text.RegularExpressions.Regex.IsMatch(
                input, @"\b\d{1,2}(:\d{2})?\s*(am|pm)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            bool hasDate = input.Contains("today", StringComparison.OrdinalIgnoreCase) ||
                           input.Contains("tomorrow", StringComparison.OrdinalIgnoreCase);

            if (!hasTime && !hasDate) return null;

            string whenPart = input.Contains(" to ", StringComparison.OrdinalIgnoreCase)
                ? input.Split(" to ", StringSplitOptions.None).Last()
                : input;

            return ParseDate(whenPart).Date + ParseTime(whenPart).TimeOfDay;
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

            return FuzzyContains(input, "update", 1) ||
                   FuzzyContains(input, "change", 1) ||
                   FuzzyContains(input, "edit", 1) ||
                   FuzzyContains(input, "modify", 1);
        }

        private bool LooksLikeReadRequest(string input)
        {
            input = input.ToLower();

            return input.Contains("do i have") ||
                   input.Contains("any tasks") ||
                   input.Contains("tasks with") ||
                   FuzzyContains(input, "show", 1) ||
                   FuzzyContains(input, "list", 1) ||
                   FuzzyContains(input, "read", 1) ||
                   FuzzyContains(input, "view", 1) ||
                   FuzzyContains(input, "display", 2) ||
                   FuzzyContains(input, "see", 1);
        }

        private bool LooksLikeDeleteRequest(string input)
        {
            input = input.ToLower();

            return FuzzyContains(input, "delete", 1) ||
                   FuzzyContains(input, "remove", 1) ||
                   FuzzyContains(input, "erase", 1) ||
                   FuzzyContains(input, "cancel", 1);
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

        private int CalculateDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return string.IsNullOrEmpty(target) ? 0 : target.Length;
            if (string.IsNullOrEmpty(target)) return source.Length;

            int[,] distance = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; distance[i, 0] = i++) ;
            for (int j = 0; j <= target.Length; distance[0, j] = j++) ;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }
            return distance[source.Length, target.Length];
        }

        private bool FuzzyContains(string text, string query, int maxDistance = 2)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query)) return false;

            text = text.ToLower();
            query = query.ToLower();

            // Direct match check first
            if (text.Contains(query)) return true;

            // Split into words and check distance for typos
            var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var qWord in queryWords)
            {
                bool wordFound = false;
                foreach (var tWord in textWords)
                {
                    // If a word is within 'maxDistance' edits, consider it a match
                    if (CalculateDistance(tWord, qWord) <= maxDistance)
                    {
                        wordFound = true;
                        break;
                    }
                }
                
                // If a significant word in the query fails entirely, return false
                if (!wordFound && qWord.Length > 2) return false;
            }

            return true; // All significant query words had a fuzzy match
        }
    }
}
