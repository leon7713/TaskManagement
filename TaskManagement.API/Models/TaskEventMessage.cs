namespace TaskManagement.API.Models
{
    public class TaskEventMessage
    {
        public int TaskId { get; set; }
        public string EventType { get; set; } = string.Empty; // Created, Updated, Deleted, Completed
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
