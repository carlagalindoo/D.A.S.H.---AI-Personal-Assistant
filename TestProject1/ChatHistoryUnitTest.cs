using Domain.Models;
using Xunit;

namespace UnitTestProject
{
    public class ChatHistoryTests
    {
        [Fact]
        public void Messages_Should_Initialize_As_Empty_List()
        {
            // Arrange
            var chatHistory = new ChatHistory();

            // Assert
            Assert.NotNull(chatHistory.Messages);
            Assert.Empty(chatHistory.Messages);
        }

        [Fact]
        public void ChatHistory_Should_Store_UserId_Correctly()
        {
            // Arrange
            var chatHistory = new ChatHistory
            {
                UserId = 5
            };

            // Assert
            Assert.Equal(5, chatHistory.UserId);
        }
    }
}