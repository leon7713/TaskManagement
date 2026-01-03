using System.Net;
using System.Net.Http.Json;
using TaskManagement.API.DTOs;
using TaskManagement.Tests.Helpers;
using Xunit;

namespace TaskManagement.Tests.Integration
{
    public class TasksApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public TasksApiIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetTasks_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/tasks");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_WithValidData_ReturnsCreated()
        {
            // Arrange
            var createTaskDto = new CreateTaskDto
            {
                Title = "Integration Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/tasks", createTaskDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdTask = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
            Assert.NotNull(createdTask);
            Assert.Equal(createTaskDto.Title, createdTask.Title);
            Assert.Equal(createTaskDto.Email, createdTask.Email);
        }

        [Fact]
        public async Task CreateTask_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var createTaskDto = new CreateTaskDto
            {
                Title = "",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "invalid-email"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/tasks", createTaskDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskById_WhenNotExists_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/tasks/99999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateAndGetTask_ReturnsCreatedTask()
        {
            // Arrange - Create a task first
            var createTaskDto = new CreateTaskDto
            {
                Title = "Test Task for Get",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createTaskDto);
            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>();

            // Act - Get the created task
            var getResponse = await _client.GetAsync($"/api/tasks/{createdTask!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var retrievedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
            Assert.NotNull(retrievedTask);
            Assert.Equal(createTaskDto.Title, retrievedTask.Title);
        }

        [Fact]
        public async Task CreateUpdateAndVerifyTask_UpdatesSuccessfully()
        {
            // Arrange - Create a task
            var createTaskDto = new CreateTaskDto
            {
                Title = "Original Title",
                Description = "Original Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createTaskDto);
            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>();

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

            // Act - Update the task
            var updateResponse = await _client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}", updateTaskDto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            // Verify the update by getting the task
            var getResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
            var updatedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
            Assert.NotNull(updatedTask);
            Assert.Equal("Updated Title", updatedTask.Title);
            Assert.True(updatedTask.IsCompleted);
        }

        [Fact]
        public async Task CreateAndDeleteTask_DeletesSuccessfully()
        {
            // Arrange - Create a task
            var createTaskDto = new CreateTaskDto
            {
                Title = "Task to Delete",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createTaskDto);
            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>();

            // Act - Delete the task
            var deleteResponse = await _client.DeleteAsync($"/api/tasks/{createdTask!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify deletion
            var getResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task GetOverdueTasks_ReturnsOnlyOverdueTasks()
        {
            // Arrange - Create an overdue task
            var overdueTaskDto = new CreateTaskDto
            {
                Title = "Overdue Task",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(-1),
                Priority = 1,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            await _client.PostAsJsonAsync("/api/tasks", overdueTaskDto);

            // Create a future task
            var futureTaskDto = new CreateTaskDto
            {
                Title = "Future Task",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                FullName = "Jane Doe",
                Telephone = "0987654321",
                Email = "jane@example.com"
            };

            await _client.PostAsJsonAsync("/api/tasks", futureTaskDto);

            // Act - Get overdue tasks
            var response = await _client.GetAsync("/api/tasks/overdue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponseDto>>();
            Assert.NotNull(tasks);
            Assert.NotEmpty(tasks);
            Assert.All(tasks, task => Assert.True(task.IsOverdue));
        }
    }
}

