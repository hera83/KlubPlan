using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.Extensions;
using web.Managers.Profile.Interfaces;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize]
public class ProfileController(
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository,
    IProfileManager profileManager,
    ILogger<ProfileController> logger) : Controller
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(user.ToProfileViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileViewModel model, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null || await userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Profilen kunne ikke opdateres. Kontroller felterne." : errors);
        }

        var result = await userRepository.UpdateProfileAsync(userId, model.EditForm.ToUpdateUserProfileDto(), cancellationToken);
        return result.Status switch
        {
            UpdateUserStatus.Updated => this.ToastSuccessJson("Profil opdateret."),
            UpdateUserStatus.NotFound => this.ToastErrorJson("Bruger ikke fundet."),
            UpdateUserStatus.UserNameTaken => this.ToastErrorJson($"Brugernavnet '{model.EditForm.UserName}' er allerede taget."),
            _ => this.ToastErrorJson(result.ErrorMessage ?? "Opdatering mislykkedes.")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTheme(string themePreference, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var validModes = new[] { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System };
        var preference = validModes.Contains(themePreference) ? themePreference : ThemeMode.System;

        if (userId is null || !await userRepository.SetThemePreferenceAsync(userId, preference, cancellationToken))
        {
            return Unauthorized();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile croppedImage, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null || await userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return Unauthorized();
        }

        if (croppedImage is null || croppedImage.Length == 0)
        {
            return BadRequest(new { error = "Ingen fil modtaget." });
        }

        if (!AllowedImageTypes.Contains(croppedImage.ContentType))
        {
            return BadRequest(new { error = "Filtype er ikke tilladt. Brug JPEG, PNG eller WebP." });
        }

        if (croppedImage.Length > MaxAvatarBytes)
        {
            return BadRequest(new { error = "Filen er for stor (max 5 MB)." });
        }

        await using var stream = croppedImage.OpenReadStream();
        var saved = await profileManager.SetAvatarAsync(userId, stream, croppedImage.ContentType, AvatarFileName(croppedImage), cancellationToken);

        if (!saved)
        {
            logger.LogWarning("Avatar save failed for user {UserId}", userId);
            return StatusCode(500, new { error = "Kunne ikke gemme billede." });
        }

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null || await userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return Unauthorized();
        }

        await profileManager.DeleteAvatarAsync(userId, cancellationToken);
        return this.ToastSuccessJson("Profilbillede fjernet.");
    }

    [HttpGet]
    public async Task<IActionResult> Avatar(string? userId, CancellationToken cancellationToken)
    {
        var targetUserId = string.IsNullOrEmpty(userId) ? userManager.GetUserId(User) : userId;
        if (string.IsNullOrEmpty(targetUserId))
        {
            return NotFound();
        }

        var file = await profileManager.GetAvatarAsync(targetUserId, cancellationToken);
        return file is null ? NotFound() : PhysicalFile(file.FullPath, file.ContentType);
    }

    private async Task<UserDto?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        return userId is null ? null : await userRepository.GetByIdAsync(userId, cancellationToken);
    }

    // The stored name only keeps the extension, so match it to the content type like the old code did (.png or .jpg).
    private static string AvatarFileName(IFormFile file) =>
        Path.GetFileNameWithoutExtension(file.FileName) + (file.ContentType.Contains("png") ? ".png" : ".jpg");
}
