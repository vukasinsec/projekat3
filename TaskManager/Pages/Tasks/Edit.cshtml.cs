using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Pages.Tasks
{
    public class EditModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;

        public EditModel(TaskService taskService, ProjectService projectService)
        {
            _taskService = taskService;
            _projectService = projectService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = null!;

        [BindProperty]
        public TaskItem Task { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            var existingTask = await _taskService.GetByIdAsync(Id);
            if (existingTask == null)
                return NotFound();

            Task = existingTask;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existingTask = await _taskService.GetByIdAsync(Id);
            if (existingTask == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await _projectService.GetByIdAsync(existingTask.ProjectId);

            if (existingTask.CreatedByUserId != currentUserId && project?.OwnerId != currentUserId)
                return Forbid();

            Task.Id = existingTask.Id; // ensure ID consistency
            Task.ProjectId = existingTask.ProjectId;
            Task.CreatedByUserId = existingTask.CreatedByUserId;

            await _taskService.UpdateAsync(Id, Task);

            TempData["SuccessMessage"] = "Task updated successfully!";
            return RedirectToPage("/Projects/Details", new { id = existingTask.ProjectId });
        }
    }
}
