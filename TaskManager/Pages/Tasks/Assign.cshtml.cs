using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Pages.Tasks
{
    public class AssignModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;

        public AssignModel(TaskService taskService, UserService userService, NotificationService notificationService)
        {
            _taskService = taskService;
            _userService = userService;
            _notificationService = notificationService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = null!;

        [BindProperty]
        public TaskItem Task { get; set; } = null!;

        [BindProperty]
        public string SelectedUserId { get; set; } = null!;

        public List<SelectListItem> UserOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Task = await _taskService.GetByIdAsync(Id);
            if (Task == null)
                return NotFound();

            var project = await _taskService.GetProjectByTaskIdAsync(Task.Id);
            if (project == null)
                return NotFound();

            var collaborators = await _userService.GetCollaboratorsForUserAsync(project.OwnerId);
            var owner = await _userService.GetByIdAsync(project.OwnerId);

            if (owner != null && !collaborators.Any(u => u.Id == owner.Id))
                collaborators.Add(owner);

            UserOptions = collaborators
                .Where(u => u.Id != Task.AssignedUserId) // <--- filtriraj da ne uključi trenutno dodeljenog
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                }).ToList();


            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var taskFromDb = await _taskService.GetByIdAsync(Task.Id);
            if (taskFromDb == null)
                return NotFound();

            var project = await _taskService.GetProjectByTaskIdAsync(taskFromDb.Id);
            if (project == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (project.OwnerId != currentUserId && taskFromDb.AssignedUserId != currentUserId)
                return Forbid(); // Ne dozvoli ako korisnik nije vlasnik ili trenutni assignovani

            // Ažuriraj task
            taskFromDb.AssignedUserId = SelectedUserId;
            await _taskService.UpdateAsync(taskFromDb.Id, taskFromDb);

            await _notificationService.CreateAsync(new Notification
            {
                UserId = SelectedUserId,
                SenderUserId = currentUserId,
                ProjectId = project.Id,
                Type = NotificationType.TaskAssigned,
                Message = $"Dodeljen ti je novi zadatak: {taskFromDb.Title}",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });


            return RedirectToPage("./Details", new { id = taskFromDb.Id });
        }

    }
}
