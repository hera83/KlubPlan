using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.ViewModels;
using web.Constants;
using web.Repositories.Users.Interfaces;

namespace web.Controllers;

/// <summary>
/// Controller for initial setup tasks
/// </summary>
[AllowAnonymous]
public class SetupController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IUserRepository userRepository,
    ILogger<SetupController> logger) : Controller
{
    /// <summary>
    /// Display first user setup form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> FirstUser(CancellationToken cancellationToken)
    {
        // If users exist, redirect to home
        if (await userRepository.AnyAsync(cancellationToken))
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    /// <summary>
    /// Create first user with Developer role
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FirstUser(FirstUserViewModel model, CancellationToken cancellationToken)
    {
        // If users exist, redirect to home
        if (await userRepository.AnyAsync(cancellationToken))
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            EmailConfirmed = true, // Auto-confirm first user
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Assign Developer role
            var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Developer);

            if (roleResult.Succeeded)
            {
                logger.LogInformation("First user created successfully with Developer role: {Email}", user.Email);

                // Sign in the user
                await signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                logger.LogError("Failed to assign Developer role to first user: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                ModelState.AddModelError(string.Empty, "Bruger oprettet, men kunne ikke tildele rolle.");
            }
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }
}
