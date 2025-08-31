using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace TaskManager.Pages
{
    public class IndexModel : PageModel
    {
        public string? UserName { get; private set; }

        public void OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                UserName = User.FindFirst(ClaimTypes.Name)?.Value;
            }
        }
    }
}
