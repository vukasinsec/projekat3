using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskManager.Models;
using TaskManager.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TaskManager.Pages.Comments
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly CommentService _commentService;

        public EditModel(CommentService commentService)
        {
            _commentService = commentService;
        }

        [BindProperty]
        public Comment Comment { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null || comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            Comment = comment;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"Logged-in userId: {userId}");

            if (userId == null)
                return Challenge();

            var existingComment = await _commentService.GetByIdAsync(Comment.Id);
            if (existingComment == null)
            {
                Console.WriteLine($"[POST] Comment.Id = {Comment?.Id}");
                Console.WriteLine($"[POST] Logged userId = {userId}");
                Console.WriteLine($"[POST] Comment from DB: {existingComment?.Id}, user = {existingComment?.UserId}");

                Console.WriteLine("Comment not found.");
                return Forbid();
            }

            Console.WriteLine($"Comment found. UserId of comment: {existingComment.UserId}");

            if (existingComment.UserId != userId)
            {
                Console.WriteLine($"[POST] Comment.Id = {Comment?.Id}");
                Console.WriteLine($"[POST] Logged userId = {userId}");
                Console.WriteLine($"[POST] Comment from DB: {existingComment?.Id}, user = {existingComment?.UserId}");

                Console.WriteLine("Comment does not belong to logged-in user.");
                return Forbid();
            }

            existingComment.Text = Comment.Text;
            await _commentService.UpdateAsync(Comment.Id, existingComment);

            return RedirectToPage("/Tasks/Details", new { id = existingComment.TaskId });
        }

    }
}
