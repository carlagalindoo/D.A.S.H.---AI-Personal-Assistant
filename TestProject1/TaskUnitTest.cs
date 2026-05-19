using Domain.Models;
using Xunit;

namespace UnitTestProject
{
    public class TaskTests
    {
        [Fact]
        public void Task_Should_Have_Default_SessionKey()
        {
            // Arrange
            var task = new Domain.Models.Task();

            // Assert
            Assert.Equal("1", task.SessionKey);
        }

        [Fact]
        public void Task_Should_Store_Title_Correctly()
        {
            // Arrange
            var task = new Domain.Models.Task
            {
                Title = "Meeting"
            };

            // Assert
            Assert.Equal("Meeting", task.Title);
        }

        [Fact]
        public void Task_Should_Store_Description_Correctly()
        {
            // Arrange
            var task = new Domain.Models.Task
            {
                Description = "Project discussion"
            };

            // Assert
            Assert.Equal("Project discussion", task.Description);
        }
    }
}