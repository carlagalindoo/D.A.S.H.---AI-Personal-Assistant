using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class MockAIService : IAIService
    {
        public Task<string> SendMessageAsync(string message)
        {
            return Task.FromResult("This is a mock response from the chatbot.");
        }
    }
}
