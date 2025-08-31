using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        private readonly ProjectService _projectService;
        private readonly CommentService _commentService;
        private readonly NotificationService _notificationService;

        public DetailsModel(
            TaskService taskService,
            UserService userService,
            ProjectService projectService,
            CommentService commentService,
            NotificationService notificationService)
        {
            _taskService = taskService;
            _userService = userService;
            _projectService = projectService;
            _commentService= commentService;
            _notificationService = notificationService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = null!;

        public TaskItem Task { get; set; } = null!;
        public User? AssignedUser { get; set; }
        public User? CreatedBy { get; set; }
        public List<Comment> Comments { get; set; } = new();
        public bool IsEditableByCurrentUser { get; set; }
        [BindProperty]
        public string NewCommentText { get; set; } = string.Empty;


        public string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> OnGetAsync()
        {
            Task = await _taskService.GetByIdAsync(Id);
            if (Task == null)
                return NotFound();

            // Dohvatanje korisnika
            if (!string.IsNullOrEmpty(Task.AssignedUserId))
                AssignedUser = await _userService.GetByIdAsync(Task.AssignedUserId);

            if (!string.IsNullOrEmpty(Task.CreatedByUserId))
                CreatedBy = await _userService.GetByIdAsync(Task.CreatedByUserId);

            // Komentari
            Comments = await _taskService.GetCommentsForTaskAsync(Id);

            // Dohvatanje imena autora komentara
            foreach (var comment in Comments)
            {
                var author = await _userService.GetByIdAsync(comment.UserId);
                comment.AuthorName = author?.UserName ?? "Unknown";
            }

            // Prava za izmenu – dohvatanje ID-a iz claim-a
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _projectService.GetByIdAsync(Task.ProjectId);

            IsEditableByCurrentUser =
                     Task.CreatedByUserId == currentUserId ||
                     (project != null && project.OwnerId == currentUserId) ||
                     Task.AssignedUserId == currentUserId;


            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var task = await _taskService.GetByIdAsync(Id);
            if (task == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(NewCommentText) || string.IsNullOrEmpty(currentUserId))
                return RedirectToPage(new { id = Id });

            var comment = new Comment
            {
                TaskId = task.Id,
                UserId = currentUserId,
                Text = NewCommentText,
                CreatedAt = DateTime.UtcNow
            };
            
            await _taskService.AddCommentAsync(comment);

            var authorUser = await _userService.GetByIdAsync(currentUserId);
            var authorName = authorUser?.UserName ?? "Nepoznat korisnik";


            // Obavesti korisnika kojem je dodeljen task (ako nije isti kao komentator)
            if (!string.IsNullOrEmpty(task.AssignedUserId) && task.AssignedUserId != currentUserId)
            {
                await _notificationService.CreateAsync(new Notification
                {
                    UserId = task.AssignedUserId,
                    SenderUserId = currentUserId,
                    ProjectId = task.ProjectId,
                    Type = NotificationType.CommentAdded,
                    Message = $"Korisnik {authorName} je komentarisao tvoj zadatak: \"{task.Title}\"",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            return RedirectToPage(new { id = Id });
        }


        public async Task<IActionResult> OnPostDeleteCommentAsync(string commentId)
        {
            var comment = await _commentService.GetByIdAsync(commentId);
            if (comment == null || comment.UserId != CurrentUserId)
            {
                return Forbid();
            }

            await _commentService.DeleteAsync(commentId);
            return RedirectToPage(new { id = Id });
        }


    }
}
