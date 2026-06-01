using Domain.Models;
using Xunit;

namespace UnitTestProject
{
    public class UserTests
    {
        [Fact]
        public void User_Should_Create_Successfully()
        {
            // Arrange
            var user = new User();

            // Assert
            Assert.NotNull(user);
        }

        [Fact]
        public void User_Should_Store_UserId_Correctly()
        {
            // Arrange
            var user = new User
            {
                UserId = 1
            };

            // Assert
            Assert.Equal(1, user.UserId);
        }
    }
}