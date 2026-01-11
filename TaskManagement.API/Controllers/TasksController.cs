using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.DTOs;
using TaskManagement.API.Interfaces;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        // GET: api/Tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetTasks()
        {
            try
            {
                var tasks = await _taskService.GetAllTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving tasks");
                return StatusCode(500, "An error occurred while retrieving tasks");
            }
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);

                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the task");
            }
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var task = await _taskService.CreateTaskAsync(createTaskDto);
                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating task");
                return StatusCode(500, "An error occurred while creating the task");
            }
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var task = await _taskService.UpdateTaskAsync(id, updateTaskDto);
                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the task");
            }
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var result = await _taskService.DeleteTaskAsync(id);
                if (!result)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the task");
            }
        }

        // GET: api/Tasks/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetOverdueTasks()
        {
            try
            {
                var tasks = await _taskService.GetOverdueTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving overdue tasks");
                return StatusCode(500, "An error occurred while retrieving overdue tasks");
            }
        }
    }
}
