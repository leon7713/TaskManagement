using TaskManagement.API.DTOs;

namespace TaskManagement.API.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync();
        Task<TaskResponseDto?> GetTaskByIdAsync(int id);
        Task<IEnumerable<TaskResponseDto>> GetOverdueTasksAsync();
        Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto createTaskDto);
        Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto);
        Task<bool> DeleteTaskAsync(int id);
    }
}
