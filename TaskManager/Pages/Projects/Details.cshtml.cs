using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskManager.Services;
using TaskManager.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManager.Pages.Projects
{
    public class DetailsModel : PageModel
    {
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;
        public DetailsModel(ProjectService projectService, TaskService taskService, UserService userService, NotificationService notificationService)
        {
            _projectService = projectService;
            _taskService = taskService;
            _userService = userService;
            _notificationService = notificationService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        public Project? Project { get; set; }
        public List<TaskItem> Tasks { get; set; } = new();
        public List<User> Collaborators { get; set; } = new();
        public User? Owner { get; set; }
        public bool CanAddTasks { get; set; } = false;

        [BindProperty]
        public string SelectedUserId { get; set; }

        public SelectList AvailableUsersSelectList { get; set; }

        public bool IsOwner
        {
            get
            {
                var currentUserId = User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return Project?.OwnerId == currentUserId;
            }
        }


        public async Task<IActionResult> OnGetAsync()
        {
            Console.WriteLine($"Project Details ID = {Id}");

            Project = await _projectService.GetByIdAsync(Id);
            if (Project == null) return NotFound();

            Tasks = await _taskService.GetByProjectIdAsync(Project.Id);
            Owner = await _userService.GetByIdAsync(Project.OwnerId);

            var currentUserId = User.FindFirst("sub")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;


            CanAddTasks = Project.OwnerId == currentUserId || Project.CollaboratorIds.Contains(currentUserId);
            
            Collaborators = new();

            // Ako trenutni korisnik NIJE vlasnik, dodaj vlasnika kao saradnika
            if (currentUserId != Project.OwnerId && Owner != null)
            {
                Collaborators.Add(Owner);
            }

            // Dodaj ostale saradnike osim trenutno ulogovanog korisnika
            foreach (var userId in Project.CollaboratorIds)
            {
                if (userId == currentUserId) continue;

                var user = await _userService.GetByIdAsync(userId);
                if (user != null)
                {
                    Collaborators.Add(user);
                }
            }
            
            Collaborators = Collaborators.Where(c => c.Id != currentUserId).ToList();

            // Učitaj sve korisnike koji nisu već kolaboratori
            var allUsers = await _userService.GetAllAsync();
            var availableUsers = allUsers
                .Where(u => u.Id != Project.OwnerId && !Project.CollaboratorIds.Contains(u.Id))
                .ToList();

            AvailableUsersSelectList = new SelectList(availableUsers, "Id", "UserName");

            return Page();
        }

        public async Task<IActionResult> OnPostAddCollaboratorAsync(string id)
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null) return NotFound();

            var currentUserId = User.FindFirst("sub")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (project.OwnerId != currentUserId)
            {
                return Forbid(); // samo vlasnik može dodati
            }

            await _projectService.AddCollaboratorAsync(id, SelectedUserId);

            var user = await _userService.GetByIdAsync(SelectedUserId);

            await _notificationService.CreateAsync(new Notification
            {
                UserId = SelectedUserId,
                SenderUserId = currentUserId,
                ProjectId = id,
                Type = NotificationType.CollaboratorAdded,
                Message = $"Dodati ste kao saradnik na projekat \"{project.Name}\"",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            TempData["SuccessMessage"] = $"Korisnik {user?.UserName} je dodat kao saradnik.";
            return RedirectToPage(new { id });
        }



    }
}
