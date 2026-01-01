namespace TaskManagement.Service.Models
{
    public class TaskReminderMessage
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime SentAt { get; set; }
    }
}
