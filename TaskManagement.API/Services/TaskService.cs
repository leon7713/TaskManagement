using TaskManagement.API.DTOs;
using TaskManagement.API.Interfaces;
using TaskManagement.API.Models;

namespace TaskManagement.API.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;
        private readonly ILogger<TaskService> _logger;
        private readonly IRabbitMQService _rabbitMQService;

        public TaskService(ITaskRepository repository, ILogger<TaskService> logger, IRabbitMQService rabbitMQService)
        {
            _repository = repository;
            _logger = logger;
            _rabbitMQService = rabbitMQService;
        }

        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
        {
            var tasks = await _repository.GetAllTasksAsync();
            return tasks.Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
        {
            var task = await _repository.GetTaskByIdAsync(id);
            return task == null ? null : MapToResponseDto(task);
        }

        public async Task<IEnumerable<TaskResponseDto>> GetOverdueTasksAsync()
        {
            var tasks = await _repository.GetOverdueTasksAsync();
            return tasks.Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto createTaskDto)
        {
            // Business logic validation can go here
            ValidateTaskDueDate(createTaskDto.DueDate);

            var task = new TaskItem
            {
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                DueDate = createTaskDto.DueDate,
                Priority = createTaskDto.Priority,
                FullName = createTaskDto.FullName,
                Telephone = createTaskDto.Telephone,
                Email = createTaskDto.Email,
                CreatedAt = DateTime.UtcNow
            };

            var createdTask = await _repository.CreateTaskAsync(task);
            _logger.LogInformation("Task created with ID {TaskId}", createdTask.Id);
            
            // Publish task created event to RabbitMQ
            PublishTaskEvent(createdTask, "Created");
            
            return MapToResponseDto(createdTask);
        }

        public async Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto)
        {
            var task = await _repository.GetTaskByIdAsync(id);
            if (task == null)
                return null;

            // Allow updating overdue tasks - no due date validation on updates
            var wasCompleted = task.IsCompleted;

            task.Title = updateTaskDto.Title;
            task.Description = updateTaskDto.Description;
            task.DueDate = updateTaskDto.DueDate;
            task.Priority = updateTaskDto.Priority;
            task.FullName = updateTaskDto.FullName;
            task.Telephone = updateTaskDto.Telephone;
            task.Email = updateTaskDto.Email;
            task.IsCompleted = updateTaskDto.IsCompleted;
            task.UpdatedAt = DateTime.UtcNow;

            var updatedTask = await _repository.UpdateTaskAsync(task);
            _logger.LogInformation("Task with ID {TaskId} updated", id);
            
            // Publish appropriate event based on completion status change
            var eventType = !wasCompleted && updateTaskDto.IsCompleted ? "Completed" : "Updated";
            PublishTaskEvent(updatedTask, eventType);
            
            return MapToResponseDto(updatedTask);
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await _repository.GetTaskByIdAsync(id);
            var result = await _repository.DeleteTaskAsync(id);
            
            if (result)
            {
                _logger.LogInformation("Task with ID {TaskId} deleted", id);
                
                // Publish task deleted event to RabbitMQ
                if (task != null)
                {
                    PublishTaskEvent(task, "Deleted");
                }
            }
            
            return result;
        }

        // Private helper methods
        private void PublishTaskEvent(TaskItem task, string eventType)
        {
            try
            {
                var message = new TaskEventMessage
                {
                    TaskId = task.Id,
                    EventType = eventType,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task.DueDate,
                    IsCompleted = task.IsCompleted,
                    Timestamp = DateTime.UtcNow
                };

                _rabbitMQService.PublishTaskEvent(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {EventType} event for task {TaskId}", eventType, task.Id);
                // Don't throw - we don't want message publishing failures to break the API
            }
        }

        private static void ValidateTaskDueDate(DateTime dueDate)
        {
            if (dueDate < DateTime.UtcNow.AddHours(-1)) // Allow 1 hour grace period
            {
                throw new InvalidOperationException("Due date cannot be in the past");
            }
        }

        private static TaskResponseDto MapToResponseDto(TaskItem task)
        {
            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                FullName = task.FullName,
                Telephone = task.Telephone,
                Email = task.Email,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                IsOverdue = task.IsOverdue,
                IsCompleted = task.IsCompleted
            };
        }
    }
}
