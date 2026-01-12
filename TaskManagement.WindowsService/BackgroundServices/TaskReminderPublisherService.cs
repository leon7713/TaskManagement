using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.WindowsService.Interfaces;
using TaskManagement.WindowsService.Models;

namespace TaskManagement.WindowsService.BackgroundServices
{
    /// <summary>
    /// Background service that periodically checks for overdue tasks
    /// and publishes reminders to RabbitMQ
    /// </summary>
    public class TaskReminderPublisherService : BackgroundService
    {
        private readonly ILogger<TaskReminderPublisherService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _checkInterval;

        public TaskReminderPublisherService(
            ILogger<TaskReminderPublisherService> logger,
            IServiceProvider serviceProvider,
            IRabbitMqService rabbitMqService,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _rabbitMqService = rabbitMqService;
            _configuration = configuration;

            // Read check interval from configuration, default to 5 minutes
            var intervalMinutes = _configuration.GetValue<int>("TaskReminder:CheckIntervalMinutes", 5);
            _checkInterval = TimeSpan.FromMinutes(intervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Task Reminder Publisher Service started. Check interval: {Interval}",
                _checkInterval
            );

            // Wait a bit before first execution to allow services to initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndPublishOverdueTasksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Task Reminder Publisher Service");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Task Reminder Publisher Service is stopping");
                    break;
                }
            }

            _logger.LogInformation("Task Reminder Publisher Service stopped");
        }

        private async Task CheckAndPublishOverdueTasksAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

            try
            {
                // Get overdue tasks (due date has passed and not completed)
                var overdueTasksQuery = context.Tasks
                    .Where(t => t.DueDate < DateTime.UtcNow && !t.IsCompleted)
                    .AsNoTracking();

                var overdueTasks = await overdueTasksQuery.ToListAsync(cancellationToken);

                if (overdueTasks.Any())
                {
                    _logger.LogInformation(
                        "Found {Count} overdue task(s) at {Time}",
                        overdueTasks.Count,
                        DateTime.UtcNow
                    );

                    foreach (var task in overdueTasks)
                    {
                        try
                        {
                            var message = new TaskReminderMessage
                            {
                                TaskId = task.Id,
                                Title = task.Title,
                                DueDate = task.DueDate,
                                FullName = task.FullName,
                                Email = task.Email,
                                ProcessedAt = DateTime.UtcNow
                            };

                            _rabbitMqService.PublishTaskReminder(message);

                            _logger.LogInformation(
                                "Published reminder for Task ID {TaskId} - '{Title}' (Due: {DueDate})",
                                task.Id,
                                task.Title,
                                task.DueDate.ToString("yyyy-MM-dd HH:mm")
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error publishing reminder for Task ID {TaskId}",
                                task.Id
                            );
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("No overdue tasks found at {Time}", DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying overdue tasks from database");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Task Reminder Publisher Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
