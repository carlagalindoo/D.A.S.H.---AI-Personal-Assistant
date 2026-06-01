using Domain.Models;
using Xunit;

namespace UnitTestProject
{
    public class MessageTests
    {
        [Fact]
        public void Message_Should_Have_Empty_Text_By_Default()
        {
            // Arrange
            var message = new Message();

            // Assert
            Assert.Equal(string.Empty, message.Text);
        }

        [Fact]
        public void Message_Should_Store_ChatHistoryId_Correctly()
        {
            // Arrange
            var message = new Message
            {
                ChatHistoryId = 10
            };

            // Assert
            Assert.Equal(10, message.ChatHistoryId);
        }

        [Fact]
        public void Message_Should_Store_Intent_Correctly()
        {
            // Arrange
            var message = new Message
            {
                Intent = IntentType.Create
            };

            // Assert
            Assert.Equal(IntentType.Create, message.Intent);
        }
    }
}