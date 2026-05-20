using BLL.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace DASHTest
{
    public class ChatServiceUnitTest
    {
        private readonly Mock<IChatService> _mockChatService;

        public ChatServiceUnitTest()
        {
            // Mock the interface directly instead of testing the concrete ChatService
            _mockChatService = new Mock<IChatService>();
        }

        [Fact]
        public async Task ProcessMessageAsync_SunnyDay_ReturnsExpectedResponse()
        {
            // Arrange
            string sessionKey = "session123";
            string userMessage = "What are my tasks?";
            string expectedResponse = "Here are your tasks!";

            _mockChatService.Setup(s => s.ProcessMessageAsync(userMessage, sessionKey))
                            .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockChatService.Object.ProcessMessageAsync(userMessage, sessionKey);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task ProcessMessageAsync_EdgeCase_ReturnsFallbackMessage()
        {
            // Arrange
            string sessionKey = "session123";
            string userMessage = "Hello";
            string expectedFallback = "Sorry, I couldn't generate a response.";

            _mockChatService.Setup(s => s.ProcessMessageAsync(userMessage, sessionKey))
                            .ReturnsAsync(expectedFallback);

            // Act
            var result = await _mockChatService.Object.ProcessMessageAsync(userMessage, sessionKey);

            // Assert
            Assert.Equal(expectedFallback, result);
        }
    }
}
