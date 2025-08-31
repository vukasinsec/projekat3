using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskManager.Services;
using TaskManager.Models;

namespace TaskManager.Pages.Users
{
    public class ViewModel : PageModel
    {
        private readonly UserService _userService;
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;

        public ViewModel(UserService userService, ProjectService projectService, TaskService taskService)
        {
            _userService = userService;
            _projectService = projectService;
            _taskService = taskService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        public User? SelectedUser { get; set; }
        public List<Project> OwnedProjects { get; set; } = new();
        public List<Project> SharedProjects { get; set; } = new();
        public int TaskCount { get; set; }
        public List<User> Collaborators { get; set; } = new();
        public Dictionary<string, int> TaskStatusCounts { get; set; } = new();
        public async Task<IActionResult> OnGetAsync()
        {
            SelectedUser = await _userService.GetByIdAsync(Id);
            if (SelectedUser == null) return NotFound();

            var allProjects = await _projectService.GetAllAsync();
            OwnedProjects = allProjects.Where(p => p.OwnerId == Id).ToList();
            SharedProjects = allProjects.Where(p => p.CollaboratorIds.Contains(Id)).ToList();

            var allTasks = await _taskService.GetAllAsync();
            TaskCount = allTasks.Count(t => t.AssignedUserId == Id);

            TaskStatusCounts = await _taskService.GetTaskStatusCountsForUserAsync(SelectedUser.Id);


            var collaboratorIds = OwnedProjects
                .SelectMany(p => p.CollaboratorIds)
                .Distinct()
                .Where(uid => uid != Id)
                .ToList();

            foreach (var cid in collaboratorIds)
            {
                var user = await _userService.GetByIdAsync(cid);
                if (user != null) Collaborators.Add(user);
            }

            return Page();
        }
    }
}
