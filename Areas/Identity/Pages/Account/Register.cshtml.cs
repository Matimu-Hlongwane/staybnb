
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Staybnb.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        logger.LogWarning(
            "DIAGNOSTIC: Staybnb registration POST handler was reached.");

        try
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning(
                    "Registration rejected because the form is invalid.");

                return Page();
            }

            var email = Input.Email.Trim();

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                user,
                Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                logger.LogWarning(
                    "Registration failed during user creation.");

                return Page();
            }

            logger.LogWarning(
                "DIAGNOSTIC: User created; assigning Guest role.");

            var roleResult = await userManager.AddToRoleAsync(
                user,
                "Guest");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                logger.LogError(
                    "Registration failed while assigning Guest role.");

                await userManager.DeleteAsync(user);

                return Page();
            }

            logger.LogWarning(
                "DIAGNOSTIC: Guest role assigned; signing user in.");

            await signInManager.SignInAsync(
                user,
                isPersistent: false);

            logger.LogWarning(
                "DIAGNOSTIC: Registration completed; redirecting home.");

            return Redirect("~/");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected exception during Staybnb registration.");

            ModelState.AddModelError(
                string.Empty,
                "Registration failed unexpectedly. Check the application terminal for details.");

            return Page();
        }
    }
}
