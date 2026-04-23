using Domain.Interfaces;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{

    public interface IChatService
    {
        Task<string> ProcessMessageAsync(string userMessage, string sessionKey);
    }
    internal class ChatService : IChatService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAIService _aiService;
        private object tasks;

        public ChatService(
            IAppointmentRepository appointmentRepository, IAIService aiService)
        {
            _appointmentRepository = appointmentRepository;
            _aiService = aiService;
        }

        public async Task<string> ProcessMessageAsync(string userMessage, string sessionKey)
        {
            // throw new NotImplementedException();
            // 1. (Optional) Get user-related data (appointments)
            var appointments = await _appointmentRepository.GetBySessionKeyAsync(sessionKey);

            // 2. Build prompt
            var prompt = BuildPrompt(userMessage, (IEnumerable<Domain.Models.Task>)tasks);

            // 3. Send to AI
            var aiResponse = await _aiService.SendMessageAsync(prompt);

            // 4. Format response
            return aiResponse?.Trim() ?? "Sorry, I couldn't generate a response.";
        }

        private string BuildPrompt(string userMessage, IEnumerable<Domain.Models.Task> tasks)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a helpful assistant for managing tasks.");
            sb.AppendLine($"User says: {userMessage}");

            if (tasks != null && tasks.Any())
            {
                sb.AppendLine("User tasks:");
                foreach (var appt in tasks)
                {
                    sb.AppendLine($"- {appt.Date} at {appt.Time}: {appt.Description}");
                }
            }

            sb.AppendLine("Reply clearly and helpfully:");

            return sb.ToString();
        }
    }
}
