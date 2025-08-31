using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskManager.Models;
using TaskManager.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

public class LoginModel : PageModel
{
    private readonly UserService _userService;

    public LoginModel(UserService userService)
    {
        _userService = userService;
    }

    [BindProperty]
    public LoginInput Input { get; set; }

    public string ErrorMessage { get; set; }

    public class LoginInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public void OnGet()
    {
        // možeš očistiti cookie ako treba
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userService.GetByEmailAsync(Input.Email);
        if (user == null)
        {
            ErrorMessage = "Ne postoji korisnik sa ovom email adresom.";
            return Page();
        }

        // Provera lozinke (pretpostavimo da je u PasswordHash plain text ili hash - implementiraj svoju proveru)
        if (user.PasswordHash != Input.Password)
        {
            ErrorMessage = "Pogrešna lozinka.";
            return Page();
        }

        // Kreiraj claims za autentifikaciju
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true, // trajna prijava
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) // trajanje kolačića
        });
        return RedirectToPage("/Users/Profile");

    }
}
