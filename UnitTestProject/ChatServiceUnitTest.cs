using BLL.Services;
using Domain.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestProject
{
    public class ChatServiceUnitTest
    {
        private readonly Mock<IAppointmentRepository> _mockRepo;
        private readonly Mock<IAIService> _mockAiService;
        private readonly ChatService _chatService;

        public ChatServiceUnitTest()
        {
            _mockRepo = new Mock<IAppointmentRepository>();
            _mockAiService = new Mock<IAIService>();
            
            _chatService = new ChatService(_mockRepo.Object, _mockAiService.Object);
        }

        [Fact] // Change to [TestMethod] if using MSTest
        public async Task ProcessMessageAsync_WhenAiReturnsResponse_ReturnsTrimmedResponse()
        {
            // Arrange
            string expectedResponse = "Here are your tasks!";
            string aiRawResponse = "   Here are your tasks!   ";
            string sessionKey = "session123";
            string userMessage = "What are my tasks?";

            // Setup repository to return an empty list of tasks for simplicity
            _mockRepo.Setup(r => r.GetBySessionKeyAsync(sessionKey))
                     .ReturnsAsync(new List<Domain.Models.Task>());

            // Setup AI service to return our raw string
            _mockAiService.Setup(ai => ai.SendMessageAsync(It.IsAny<string>()))
                          .ReturnsAsync(aiRawResponse);

            // Act
            var result = await _chatService.ProcessMessageAsync(userMessage, sessionKey);

            // Assert
            Assert.Equal(expectedResponse, result);
            
            // Verify repository was called exactly once
            _mockRepo.Verify(r => r.GetBySessionKeyAsync(sessionKey), Times.Once);
        }

        [Fact] 
        public async Task ProcessMessageAsync_WhenAiReturnsNull_ReturnsFallbackMessage()
        {
            // Arrange
            string expectedFallback = "Sorry, I couldn't generate a response.";
            string sessionKey = "session123";

            _mockRepo.Setup(r => r.GetBySessionKeyAsync(sessionKey))
                     .ReturnsAsync(new List<Domain.Models.Task>());

            // Setup AI service to simulate a failure/null response
            _mockAiService.Setup(ai => ai.SendMessageAsync(It.IsAny<string>()))
                          .ReturnsAsync((string)null);

            // Act
            var result = await _chatService.ProcessMessageAsync("Hello", sessionKey);

            // Assert
            Assert.Equal(expectedFallback, result);
        }

        [Fact]
        public async Task ProcessMessageAsync_BuildsPromptCorrectlyWithTasks()
        {
            // Arrange
            string sessionKey = "session123";
            string userMessage = "Check tasks";
            
            var userTasks = new List<Domain.Models.Task>
            {
                new Domain.Models.Task { Date = "2024-05-01", Time = "10:00", Description = "Team Meeting" }
            };

            // Assuming GetBySessionKeyAsync returns IEnumerable<Domain.Models.Task> once you fix the bug
            _mockRepo.Setup(r => r.GetBySessionKeyAsync(sessionKey))
                     .ReturnsAsync(userTasks);

            string capturedPrompt = string.Empty;

            // Capture the prompt string that is sent to the AI service
            _mockAiService.Setup(ai => ai.SendMessageAsync(It.IsAny<string>()))
                          .Callback<string>(prompt => capturedPrompt = prompt)
                          .ReturnsAsync("OK");

            // Act
            await _chatService.ProcessMessageAsync(userMessage, sessionKey);

            // Assert
            Assert.Contains("User says: Check tasks", capturedPrompt);
            Assert.Contains("Team Meeting", capturedPrompt);
            Assert.Contains("2024-05-01 at 10:00", capturedPrompt);
        }
    }
}
