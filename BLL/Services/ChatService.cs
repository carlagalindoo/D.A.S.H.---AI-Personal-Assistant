using DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{

    public interface IChatService
    {
        Task<string> ProcessMessageAsync(string userMessage, int userId);
    }
    internal class ChatService : IChatService
    {
        private readonly IAppointmentRepository _appointmentRepository;

        public ChatService(IAppointmentRepository appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        public Task<string> ProcessMessageAsync(string userMessage, int userId)
        {
            throw new NotImplementedException();
        }
    }
}
