using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TaskManager.Models;
using TaskManager.Services;
using UserModel = TaskManager.Models.User;

namespace TaskManager.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserService _userService;

        public RegisterModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public RegisterInput Input { get; set; } = new RegisterInput();

        public string ErrorMessage { get; set; } = "";

        public class RegisterInput
        {
            [Required]
            [Display(Name = "User Name")]
            public string UserName { get; set; } = null!;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = null!;

            [Required]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = null!;
            [BindProperty]
            public IFormFile? ProfileImageFile { get; set; }


            [Display(Name = "Bio")]
            public string? Bio { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var existingUser = await _userService.GetByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "This email is already registered.");
                return Page();
            }

            var allUsers = await _userService.GetAllAsync();
            bool isFirstUser = allUsers.Count == 0;

            string? profileImagePath = null;

            if (Input.ProfileImageFile != null && Input.ProfileImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(Input.ProfileImageFile.FileName);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");

                // Ako folder ne postoji, napravi ga
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfileImageFile.CopyToAsync(stream);
                }

                // Sačuvaj relativni put za prikaz na webu
                profileImagePath = "/images/profiles/" + fileName;
            }

            var newUser = new UserModel
            {
                UserName = Input.UserName,
                Email = Input.Email,
                PasswordHash = Input.Password,  // kasnije hashiraj
                ProfileImageUrl = profileImagePath,
                Bio = Input.Bio,
                IsAdmin = isFirstUser
            };

            await _userService.CreateAsync(newUser);

            return RedirectToPage("/Account/Login");
        }

    }
}
