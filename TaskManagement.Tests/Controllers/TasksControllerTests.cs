using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.API.Controllers;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
using Xunit;

namespace TaskManagement.Tests.Controllers
{
    public class TasksControllerTests
    {
        private readonly Mock<ILogger<TasksController>> _mockLogger;
        private readonly TaskManagementDbContext _context;
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            _mockLogger = new Mock<ILogger<TasksController>>();

            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TaskManagementDbContext(options);
            _controller = new TasksController(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task GetTasks_ReturnsOkResult_WithListOfTasks()
        {
            // Arrange
            var task1 = new TaskItem
            {
                Title = "Task 1",
                Description = "Description 1",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                CreatedAt = DateTime.UtcNow
            };

            var task2 = new TaskItem
            {
                Title = "Task 2",
                Description = "Description 2",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = 2,
                FullName = "Jane Doe",
                Telephone = "0987654321",
                Email = "jane@example.com",
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.AddRange(task1, task2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetTasks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var tasks = Assert.IsAssignableFrom<List<TaskResponseDto>>(okResult.Value);
            Assert.Equal(2, tasks.Count);
        }

        [Fact]
        public async Task GetTasks_ReturnsEmptyList_WhenNoTasks()
        {
            // Act
            var result = await _controller.GetTasks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var tasks = Assert.IsAssignableFrom<List<TaskResponseDto>>(okResult.Value);
            Assert.Empty(tasks);
        }

        [Fact]
        public async Task GetTask_ReturnsOkResult_WhenTaskExists()
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetTask(task.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var taskDto = Assert.IsType<TaskResponseDto>(okResult.Value);
            Assert.Equal(task.Title, taskDto.Title);
            Assert.Equal(task.Email, taskDto.Email);
        }

        [Fact]
        public async Task GetTask_ReturnsNotFound_WhenTaskDoesNotExist()
        {
            // Act
            var result = await _controller.GetTask(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Task with ID 999 not found", notFoundResult.Value);
        }

        [Fact]
        public async Task CreateTask_ReturnsCreatedAtAction_WithValidData()
        {
            // Arrange
            var createTaskDto = new CreateTaskDto
            {
                Title = "New Task",
                Description = "New Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Act
            var result = await _controller.CreateTask(createTaskDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var taskDto = Assert.IsType<TaskResponseDto>(createdResult.Value);
            Assert.Equal(createTaskDto.Title, taskDto.Title);
            Assert.Equal(createTaskDto.Email, taskDto.Email);
            Assert.Equal(1, await _context.Tasks.CountAsync());
        }

        [Fact]
        public async Task UpdateTask_ReturnsNoContent_WhenTaskExists()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Old Title",
                Description = "Old Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var updateTaskDto = new UpdateTaskDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = 2,
                FullName = "Jane Doe",
                Telephone = "0987654321",
                Email = "jane@example.com",
                IsCompleted = true
            };

            // Act
            var result = await _controller.UpdateTask(task.Id, updateTaskDto);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var updatedTask = await _context.Tasks.FindAsync(task.Id);
            Assert.NotNull(updatedTask);
            Assert.Equal("Updated Title", updatedTask.Title);
            Assert.Equal("jane@example.com", updatedTask.Email);
            Assert.True(updatedTask.IsCompleted);
            Assert.NotNull(updatedTask.UpdatedAt);
        }

        [Fact]
        public async Task UpdateTask_ReturnsNotFound_WhenTaskDoesNotExist()
        {
            // Arrange
            var updateTaskDto = new UpdateTaskDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = 2,
                FullName = "Jane Doe",
                Telephone = "0987654321",
                Email = "jane@example.com",
                IsCompleted = true
            };

            // Act
            var result = await _controller.UpdateTask(999, updateTaskDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Task with ID 999 not found", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteTask_ReturnsNoContent_WhenTaskExists()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Task to Delete",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            var taskId = task.Id;

            // Act
            var result = await _controller.DeleteTask(taskId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Tasks.FindAsync(taskId));
        }

        [Fact]
        public async Task DeleteTask_ReturnsNotFound_WhenTaskDoesNotExist()
        {
            // Act
            var result = await _controller.DeleteTask(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Task with ID 999 not found", notFoundResult.Value);
        }

        [Fact]
        public async Task GetOverdueTasks_ReturnsOnlyOverdueIncompleteTasks()
        {
            // Arrange
            var overdueTask = new TaskItem
            {
                Title = "Overdue Task",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(-1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var completedOverdueTask = new TaskItem
            {
                Title = "Completed Overdue Task",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(-1),
                Priority = 1,
                FullName = "Jane Doe",
                Telephone = "0987654321",
                Email = "jane@example.com",
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow
            };

            var futureTask = new TaskItem
            {
                Title = "Future Task",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "Bob Smith",
                Telephone = "1122334455",
                Email = "bob@example.com",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.AddRange(overdueTask, completedOverdueTask, futureTask);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetOverdueTasks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var tasks = Assert.IsAssignableFrom<List<TaskResponseDto>>(okResult.Value);
            Assert.Single(tasks);
            Assert.Equal("Overdue Task", tasks[0].Title);
        }
    }
}
