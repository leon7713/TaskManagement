namespace TaskManagement.WindowsService.Models
{
    /// <summary>
    /// Message model for task reminders sent through RabbitMQ
    /// </summary>
    public class TaskReminderMessage
    {
        public int TaskId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        
        public DateTime DueDate { get; set; }
        
        public string FullName { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public DateTime ProcessedAt { get; set; }
    }
}
