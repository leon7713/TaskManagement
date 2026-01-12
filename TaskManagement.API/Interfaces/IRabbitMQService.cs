using TaskManagement.API.Models;

namespace TaskManagement.API.Interfaces
{
    public interface IRabbitMQService
    {
        void PublishTaskEvent(TaskEventMessage message);
    }
}
