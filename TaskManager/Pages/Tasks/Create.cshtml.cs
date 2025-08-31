using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskManager.Models;
using TaskManager.Services;
using TaskStatus = TaskManager.Models.TaskStatus;

namespace TaskManager.Pages.Tasks
{
    public class CreateModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;

        public CreateModel(TaskService taskService, ProjectService projectService)
        {
            _taskService = taskService;
            _projectService = projectService;
        }

        [BindProperty(SupportsGet = true)]
        public string ProjectId { get; set; }

        [BindProperty]
        public TaskItem Task { get; set; }

        public IActionResult OnGet()
        {
            Console.WriteLine($"OnGet: ProjectId = {ProjectId}");
            Task = new TaskItem
            {
                ProjectId = ProjectId
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Task.CreatedByUserId");

            if (!ModelState.IsValid)
            {
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var state = ModelState[modelStateKey];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"ModelState error on '{modelStateKey}': {error.ErrorMessage}");
                    }
                }
                return Page();
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                Console.WriteLine("User not logged in");
                return Unauthorized();
            }

            // Postavi polja koja nisu u formi
            Task.CreatedByUserId = currentUserId;
            Task.AssignedUserId = currentUserId;
            Task.Status = TaskStatus.ToDo;

            try
            {
                await _taskService.CreateAsync(Task);
                await _projectService.AddTaskToProjectAsync(Task.ProjectId, Task.Id);
                Console.WriteLine($"Task created with ID: {Task.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when creating task: {ex.Message}");
                throw;
            }

            return RedirectToPage("/Projects/Details", new { id = Task.ProjectId });
        }
    }

}
