using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Extensions;
using web.Managers.InfoScreens.Interfaces;
using web.Repositories.InfoScreens.Interfaces;
using web.ViewModels;

namespace web.Controllers;

/// <summary>
/// Public, unauthenticated info screen links: /Infoskaerm?Id={screen.PublicId} — opened on the
/// projector/TV's browser, which then plays the slideshow on its own and keeps it up to date
/// (activation, saved changes). Id is InfoScreen.PublicId, so screens can't be found by guessing.
/// "preview" (the designer's Afspil) also plays an inactive screen, but only for logged-in users.
/// </summary>
[AllowAnonymous]
public class InfoskaermController(IInfoScreenRepository infoScreenRepository, IInfoScreenManager infoScreenManager) : Controller
{
    public async Task<IActionResult> Index(Guid id, bool preview, int slide, CancellationToken cancellationToken)
    {
        var title = await infoScreenRepository.GetTitleAsync(id, cancellationToken);
        return View(new InfoScreenPlayerPageViewModel
        {
            PublicId = id,
            Title = title ?? "Infoskærm",
            Preview = preview && IsLoggedIn,
            StartSlide = Math.Max(1, slide)
        });
    }

    /// <summary>The slides to play (JSON) — loaded on start and re-checked every InfoScreenRules.PollSeconds.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Data(Guid id, bool preview, CancellationToken cancellationToken)
    {
        var data = await infoScreenRepository.GetPlayerAsync(id, preview && IsLoggedIn, cancellationToken);
        return Json(data);
    }

    /// <summary>An image/video from a screen's media library. Range requests let videos stream and seek.</summary>
    [HttpGet]
    public async Task<IActionResult> Media(Guid id, CancellationToken cancellationToken)
    {
        var file = await infoScreenManager.GetMediaFileAsync(id, cancellationToken);
        if (file is null)
            return NotFound();

        // A media id always points at the same file, so the screen may cache it for a long time.
        Response.Headers.CacheControl = "public, max-age=604800";
        return PhysicalFile(file.FullPath, file.ContentType, enableRangeProcessing: true);
    }

    /// <summary>A QR code as SVG for a QR element (designer and running screen).</summary>
    [HttpGet]
    public IActionResult Qr(string? text, string? color)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0 || text.Length > InfoScreenRules.MaxQrTextLength)
            return BadRequest();

        var dark = color is not null && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") ? color : "#000000";
        // Same text + color always gives the same code.
        Response.Headers.CacheControl = "public, max-age=604800";
        return Content(QrCodeHelper.Create(text, dark), "image/svg+xml");
    }

    private bool IsLoggedIn => User.Identity?.IsAuthenticated == true;
}
