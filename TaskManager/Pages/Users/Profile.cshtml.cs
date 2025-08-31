using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManager.Hubs;
using TaskManager.Models;
using TaskManager.Services;
using UserModel = TaskManager.Models.User;

namespace TaskManager.Pages.Users
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ProfileModel : PageModel
    {
        private readonly UserService _userService;
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;
        private readonly NotificationService _notificationService;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public ProfileModel(UserService userService,
                            ProjectService projectService,
                            TaskService taskService,
                            NotificationService notificationService
                            ,IHubContext<NotificationsHub> hubContext)
        {
            _userService = userService;
            _projectService = projectService;
            _taskService = taskService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        // Prikaz podataka
        public UserModel CurrentUser { get; set; } = null!;
        public Dictionary<string, int> TaskStatistics { get; set; } = new();
        public List<UserModel> Collaborators { get; set; } = new();
        public List<Project> OwnedProjects { get; set; } = new();
        public List<Project> SharedProjects { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public List<Notification> CollaborationRequests { get; set; } = new();


        public string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // Form fields
        [BindProperty]
        public string ProjectName { get; set; } = string.Empty;

        [BindProperty]
        public string ProjectDescription { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedProjectId { get; set; } = string.Empty;
        public Dictionary<string,int> TaskStatusCounts { get; set; } = new();
        public async Task OnGetAsync()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                RedirectToPage("/Account/Login");
                return;
            }

            CurrentUser = await _userService.GetByIdAsync(CurrentUserId)
                ?? throw new Exception("User not found");

            TaskStatistics = await _projectService.GetTaskStatisticsAsync(CurrentUserId);
            Collaborators = await _userService.GetCollaboratorsForUserAsync(CurrentUserId);
            OwnedProjects = await _projectService.GetProjectsByOwnerAsync(CurrentUserId);
            SharedProjects = await _projectService.GetProjectsByCollaboratorAsync(CurrentUserId);
            Notifications = await _notificationService.GetByUserIdAsync(CurrentUserId);
            CollaborationRequests = (await _notificationService.GetByUserIdAsync(CurrentUser.Id))
            .Where(n => n.Type == NotificationType.CollaborationRequest && !n.IsRead)
            .ToList();

            TaskStatusCounts =await _taskService.GetTaskStatusCountsForUserAsync(CurrentUserId);

        }

        public async Task<IActionResult> OnPostCreateProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(ProjectDescription))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return Page();
            }

            var newProject = new Project
            {
                Name = ProjectName,
                Description = ProjectDescription,
                OwnerId = CurrentUserId,
                CollaboratorIds = new List<string>(),
                TaskIds = new List<string>(),
                PendingCollaboratorIds = new List<string>()
            };

            await _projectService.CreateAsync(newProject);
            return RedirectToPage(); // refresh profila
        }

        public async Task<IActionResult> OnPostRequestCollaborationAsync()
        {
            Console.WriteLine("SelectedProjectId: " + SelectedProjectId);
            Console.WriteLine("CurrentUserId: " + CurrentUserId);

           
            if (string.IsNullOrEmpty(SelectedProjectId) || string.IsNullOrEmpty(CurrentUserId))


            {
                ModelState.AddModelError(string.Empty, "Invalid request.");
                return Page();
            }

            CurrentUser = await _userService.GetByIdAsync(CurrentUserId);
            if (CurrentUser == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            var project = await _projectService.GetByIdAsync(SelectedProjectId);
            if (project == null)
            {
                ModelState.AddModelError(string.Empty, "Project not found.");
                return Page();
            }

            // Provera: da li je korisnik vlasnik projekta
            if (project.OwnerId == CurrentUserId)
            {
                ModelState.AddModelError(string.Empty, "You are the owner of this project.");
                return Page();
            }

            // Provera: da li je već saradnik
            if (project.CollaboratorIds.Contains(CurrentUserId))
            {
                ModelState.AddModelError(string.Empty, "You are already a collaborator on this project.");
                return Page();
            }

            // Provera: da li je već poslao zahtev
            if (project.PendingCollaboratorIds.Contains(CurrentUserId))
            {
                ModelState.AddModelError(string.Empty, "You have already sent a collaboration request for this project.");
                return Page();
            }

            var success = await _projectService.SendCollaborationRequestAsync(SelectedProjectId, CurrentUserId);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to request collaboration.");
                return Page();
            }
            await _notificationService.CreateAsync(new Notification
            {
                UserId = project.OwnerId, // kome ide notifikacija (vlasniku)
                Message = $"{CurrentUser.UserName} je poslao zahtev za saradnju na projektu '{project.Name}'",
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.CollaborationRequest,
                IsRead = false,
                ProjectId = project.Id,          // ID projekta za koji je zahtev
                SenderUserId = CurrentUserId    // ko šalje zahtev
            });
            // Pošalji real-time notifikaciju preko SignalR
            await _hubContext.Clients.User(project.OwnerId)
                .SendAsync("ReceiveNotification", new
                {
                    Message = $"{CurrentUser.UserName} je poslao zahtev za saradnju na projektu '{project.Name}'",
                    ProjectId = project.Id,
                    Type = "CollaborationRequest"
                });
            // Uspešno poslata zahtev - možeš preusmeriti ili prikazati poruku
            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetSearchProjectsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new JsonResult(new List<object>());

            var allMatchingProjects = await _projectService.SearchProjectsByNameAsync(query);

            var result = allMatchingProjects
                     .Where(p =>
                         p.OwnerId != CurrentUserId &&
                         !p.CollaboratorIds.Contains(CurrentUserId) &&
                         !p.PendingCollaboratorIds.Contains(CurrentUserId))
                     .Select(p => new {
                         id = p.Id,
                         name = p.Name,
                         description = p.Description
                     })
                     .ToList();


            return new JsonResult(result);
        }
        public async Task<IActionResult> OnPostAcceptRequestAsync(string notificationId)
        {
            await _notificationService.AcceptCollaborationRequestAsync(notificationId);
            TempData["Message"] = "Collaboration request accepted.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectRequestAsync(string notificationId)
        {
            await _notificationService.RejectCollaborationRequestAsync(notificationId);
            TempData["Message"] = "Collaboration request rejected.";
            return RedirectToPage();
        }

    }
}
