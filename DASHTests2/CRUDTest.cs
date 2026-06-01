using DAL.Data;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UnitTestProject
{
    public class TaskRepositoryTests
    {
        private AssistantDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AssistantDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;

            return new AssistantDbContext(options);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddAsync_Should_Add_Task()
        {
            // Arrange
            var context = CreateContext();
            var repository = new TaskRepository(context);

            var task = new Domain.Models.Task
            {
                Title = "Meeting",
                Description = "Project discussion",
                Location = "Office",
                People = "John",
                Date = DateTime.Now,
                Time = new TimeSpan(10, 0, 0)
            };

            // Act
            await repository.AddAsync(task);

            // Assert
            Assert.Single(context.Tasks);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_Should_Return_Task()
        {
            // Arrange
            var context = CreateContext();

            var task = new Domain.Models.Task
            {
                Title = "Meeting",
                Description = "Project discussion",
                Location = "Office",
                People = "John",
                Date = DateTime.Now,
                Time = new TimeSpan(10, 0, 0)
            };

            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var repository = new TaskRepository(context);

            // Act
            var result = await repository.GetByIdAsync(task.TaskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Meeting", result.Title);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_Should_Return_All_Tasks()
        {
            // Arrange
            var context = CreateContext();

            context.Tasks.Add(new Domain.Models.Task
            {
                Title = "Task 1",
                Description = "Description 1",
                Location = "Room A",
                People = "Alice",
                Date = DateTime.Now,
                Time = new TimeSpan(9, 0, 0)
            });

            context.Tasks.Add(new Domain.Models.Task
            {
                Title = "Task 2",
                Description = "Description 2",
                Location = "Room B",
                People = "Bob",
                Date = DateTime.Now,
                Time = new TimeSpan(11, 0, 0)
            });

            await context.SaveChangesAsync();

            var repository = new TaskRepository(context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_Should_Update_Task()
        {
            // Arrange
            var context = CreateContext();

            var task = new Domain.Models.Task
            {
                Title = "Old Title",
                Description = "Description",
                Location = "Office",
                People = "John",
                Date = DateTime.Now,
                Time = new TimeSpan(10, 0, 0)
            };

            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var repository = new TaskRepository(context);

            // Act
            task.Title = "New Title";
            await repository.UpdateAsync(task);

            // Assert
            var updatedTask = await context.Tasks.FindAsync(task.TaskId);

            Assert.NotNull(updatedTask);
            Assert.Equal("New Title", updatedTask.Title);
        }

        [Fact]
        public async System.Threading.Tasks.Task DeleteAsync_Should_Remove_Task()
        {
            // Arrange
            var context = CreateContext();

            var task = new Domain.Models.Task
            {
                Title = "Delete Me",
                Description = "Description",
                Location = "Office",
                People = "John",
                Date = DateTime.Now,
                Time = new TimeSpan(10, 0, 0)
            };

            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var repository = new TaskRepository(context);

            // Act
            await repository.DeleteAsync(task.TaskId);

            // Assert
            Assert.Empty(context.Tasks);
        }
    }
}