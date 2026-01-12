using TaskManagement.WindowsService.Models;

namespace TaskManagement.WindowsService.Interfaces
{
    /// <summary>
    /// Interface for RabbitMQ operations
    /// </summary>
    public interface IRabbitMqService : IDisposable
    {
        /// <summary>
        /// Publishes a task reminder message to the queue
        /// </summary>
        void PublishTaskReminder(TaskReminderMessage message);
        
        /// <summary>
        /// Starts consuming messages from the queue
        /// </summary>
        /// <param name="onMessageReceived">Callback when message is received</param>
        void StartConsuming(Action<TaskReminderMessage> onMessageReceived);
    }
}
