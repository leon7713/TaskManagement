using Microsoft.EntityFrameworkCore;
using TaskManagement.Service.Data;
using TaskManagement.Service.Models;
using Newtonsoft.Json;

namespace TaskManagement.Service.Services
{
    public class TaskReminderWorker : BackgroundService
    {
        private readonly ILogger<TaskReminderWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly string _queueName = "task-reminders";
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public TaskReminderWorker(
            ILogger<TaskReminderWorker> logger,
            IServiceProvider serviceProvider,
            IRabbitMQService rabbitMQService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _rabbitMQService = rabbitMQService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Task Reminder Worker started at: {time}", DateTimeOffset.Now);

            StartMessageConsumer();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckOverdueTasks(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Task Reminder Worker");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Task Reminder Worker stopping at: {time}", DateTimeOffset.Now);
        }

        private async Task CheckOverdueTasks(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

            try
            {
                var overdueTasks = await dbContext.Tasks
                    .Where(t => t.DueDate < DateTime.UtcNow && !t.IsCompleted)
                    .ToListAsync(stoppingToken);

                if (overdueTasks.Any())
                {
                    _logger.LogInformation("Found {Count} overdue tasks", overdueTasks.Count);

                    foreach (var task in overdueTasks)
                    {
                        PublishTaskReminder(task);
                    }
                }
                else
                {
                    _logger.LogDebug("No overdue tasks found at {Time}", DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overdue tasks");
            }
        }

        private void PublishTaskReminder(TaskItem task)
        {
            try
            {
                var reminderMessage = new TaskReminderMessage
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    AssignedTo = task.FullName,
                    DueDate = task.DueDate,
                    SentAt = DateTime.UtcNow
                };

                _rabbitMQService.PublishMessage(reminderMessage, _queueName);
                _logger.LogInformation("Published reminder for Task ID: {TaskId} - {Title}", task.Id, task.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish reminder for Task ID: {TaskId}", task.Id);
            }
        }

        private void StartMessageConsumer()
        {
            try
            {
                _rabbitMQService.StartConsuming(_queueName, OnReminderMessageReceived);
                _logger.LogInformation("Started listening for task reminder messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start message consumer");
            }
        }

        private void OnReminderMessageReceived(string messageJson)
        {
            try
            {
                var reminder = JsonConvert.DeserializeObject<TaskReminderMessage>(messageJson);
                if (reminder != null)
                {
                    _logger.LogWarning(
                        "TASK REMINDER: Hi {AssignedTo}, your Task is due - Task ID: {TaskId}, Title: '{Title}', Due Date: {DueDate}",
                        reminder.AssignedTo,
                        reminder.TaskId,
                        reminder.Title,
                        reminder.DueDate
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminder message: {Message}", messageJson);
            }
        }

        public override void Dispose()
        {
            _rabbitMQService?.Dispose();
            base.Dispose();
        }
    }
}
