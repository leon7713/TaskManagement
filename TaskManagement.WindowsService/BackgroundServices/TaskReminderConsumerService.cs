using System.Collections.Concurrent;
using TaskManagement.WindowsService.Interfaces;
using TaskManagement.WindowsService.Models;

namespace TaskManagement.WindowsService.BackgroundServices
{
    /// <summary>
    /// Background service that consumes task reminders from RabbitMQ
    /// and logs them (with deduplication to handle concurrent updates)
    /// </summary>
    public class TaskReminderConsumerService : BackgroundService
    {
        private readonly ILogger<TaskReminderConsumerService> _logger;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ConcurrentDictionary<int, DateTime> _processedTasks;
        private readonly TimeSpan _deduplicationWindow;

        public TaskReminderConsumerService(
            ILogger<TaskReminderConsumerService> logger,
            IRabbitMqService rabbitMqService,
            IConfiguration configuration)
        {
            _logger = logger;
            _rabbitMqService = rabbitMqService;
            _processedTasks = new ConcurrentDictionary<int, DateTime>();
            
            // Deduplication window: don't process the same task multiple times within this window
            var windowMinutes = configuration.GetValue<int>("TaskReminder:DeduplicationWindowMinutes", 60);
            _deduplicationWindow = TimeSpan.FromMinutes(windowMinutes);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Task Reminder Consumer Service started");

            try
            {
                // Start consuming messages from RabbitMQ
                _rabbitMqService.StartConsuming(OnReminderReceived);
                
                // Start background cleanup task for processed messages
                _ = Task.Run(() => CleanupProcessedTasksAsync(stoppingToken), stoppingToken);

                _logger.LogInformation("Successfully subscribed to task reminders queue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consuming task reminders");
                throw;
            }

            return Task.CompletedTask;
        }

        private void OnReminderReceived(TaskReminderMessage message)
        {
            try
            {
                // Handle concurrent updates: check if we've recently processed this task
                if (_processedTasks.TryGetValue(message.TaskId, out var lastProcessed))
                {
                    if (DateTime.UtcNow - lastProcessed < _deduplicationWindow)
                    {
                        _logger.LogDebug(
                            "Skipping duplicate reminder for Task ID {TaskId} (last processed {LastProcessed})",
                            message.TaskId,
                            lastProcessed
                        );
                        return;
                    }
                }

                // Log the reminder as specified in requirements
                _logger.LogWarning(
                    "Hi your Task is due {{Task {TaskId} - {Title}}} - Due Date: {DueDate}, Assigned to: {FullName} ({Email})",
                    message.TaskId,
                    message.Title,
                    message.DueDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    message.FullName,
                    message.Email
                );

                // Mark task as processed with current timestamp
                _processedTasks.AddOrUpdate(
                    message.TaskId,
                    DateTime.UtcNow,
                    (key, oldValue) => DateTime.UtcNow
                );

                // Additional actions can be added here:
                // - Send email notification
                // - Send SMS via Twilio
                // - Update database with "reminder sent" flag
                // - Trigger other workflows
                // - Push notification to mobile app
                
                _logger.LogInformation(
                    "Successfully processed reminder for Task ID {TaskId}",
                    message.TaskId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing reminder for Task ID {TaskId}",
                    message.TaskId
                );
                throw; // Re-throw to trigger message requeue in RabbitMQ
            }
        }

        private async Task CleanupProcessedTasksAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Clean up processed tasks older than the deduplication window
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

                    var cutoffTime = DateTime.UtcNow - _deduplicationWindow;
                    var expiredKeys = _processedTasks
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _processedTasks.TryRemove(key, out _);
                    }

                    if (expiredKeys.Any())
                    {
                        _logger.LogDebug(
                            "Cleaned up {Count} expired task entries from deduplication cache",
                            expiredKeys.Count
                        );
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup of processed tasks");
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Task Reminder Consumer Service is stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}
