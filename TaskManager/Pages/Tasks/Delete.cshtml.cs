using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Pages.Tasks
{
    public class DeleteModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;

        public DeleteModel(TaskService taskService, ProjectService projectService)
        {
            _taskService = taskService;
            _projectService = projectService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = null!;

        public TaskItem Task { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            Task = await _taskService.GetByIdAsync(Id);
            if (Task == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var task = await _taskService.GetByIdAsync(Id);
            if (task == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await _projectService.GetByIdAsync(task.ProjectId);

            if (task.CreatedByUserId != currentUserId && project?.OwnerId != currentUserId)
                return Forbid();

            await _taskService.DeleteAsync(Id);
            TempData["SuccessMessage"] = "Task deleted.";

            return RedirectToPage("/Projects/Details", new { id = task.ProjectId });
        }
    }
}
