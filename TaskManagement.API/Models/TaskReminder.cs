using System.ComponentModel.DataAnnotations;

namespace TaskManagement.API.Models
{
    /// <summary>
    /// Tracks when reminders have been sent for tasks
    /// Used for deduplication and preventing duplicate notifications
    /// </summary>
    public class TaskReminder
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [Required]
        public DateTime SentAt { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Sent"; // "Sent", "Processed", "Failed"

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation property
        public TaskItem? Task { get; set; }
    }
}
