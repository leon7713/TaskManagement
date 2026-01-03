using TaskManagement.API.Models;
using Xunit;

namespace TaskManagement.Tests.Models
{
    public class TaskItemTests
    {
        [Fact]
        public void IsOverdue_ReturnsFalse_WhenDueDateIsInFuture()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                IsCompleted = false
            };

            // Act & Assert
            Assert.False(task.IsOverdue);
        }

        [Fact]
        public void IsOverdue_ReturnsTrue_WhenDueDateIsPastAndNotCompleted()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(-1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                IsCompleted = false
            };

            // Act & Assert
            Assert.True(task.IsOverdue);
        }

        [Fact]
        public void IsOverdue_ReturnsFalse_WhenDueDateIsPastButCompleted()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(-1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                IsCompleted = true
            };

            // Act & Assert
            Assert.False(task.IsOverdue);
        }

        [Fact]
        public void CreatedAt_IsSetToUtcNow_ByDefault()
        {
            // Arrange & Act
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Assert
            Assert.True(task.CreatedAt <= DateTime.UtcNow);
            Assert.True(task.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
        }

        [Fact]
        public void IsCompleted_IsFalse_ByDefault()
        {
            // Arrange & Act
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Assert
            Assert.False(task.IsCompleted);
        }

        [Fact]
        public void UpdatedAt_IsNull_ByDefault()
        {
            // Arrange & Act
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Assert
            Assert.Null(task.UpdatedAt);
        }
    }
}
