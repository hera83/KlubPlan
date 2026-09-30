using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Infrastructure;
using web.Repositories.InfoScreens.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Public, unauthenticated info screen links: /Infoskaerm?Id={screen.PublicId} — opened on the
    /// projector/TV's browser, which then plays the slideshow on its own and keeps it up to date
    /// (activation, saved changes). Id is InfoScreen.PublicId, so screens can't be found by guessing.
    /// "preview" (the designer's Afspil) also plays an inactive screen, but only for logged-in users.
    /// </summary>
    [AllowAnonymous]
    public class InfoskaermController : Controller
    {
        private readonly IInfoScreenService _screenService;

        public InfoskaermController(IInfoScreenService screenService)
        {
            _screenService = screenService;
        }

        public async Task<IActionResult> Index(Guid id, bool preview = false, int slide = 1, CancellationToken ct = default)
        {
            var title = await _screenService.GetTitleAsync(id, ct);
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
        public async Task<IActionResult> Data(Guid id, bool preview = false, CancellationToken ct = default)
        {
            var data = await _screenService.GetPlayerDataAsync(id, preview && IsLoggedIn, ct);
            return Json(data);
        }

        /// <summary>An image/video from a screen's media library. Range requests let videos stream and seek.</summary>
        [HttpGet]
        public async Task<IActionResult> Media(Guid id, CancellationToken ct)
        {
            var file = await _screenService.GetMediaFileAsync(id, ct);
            if (file is null)
                return NotFound();

            // A media id always points at the same file, so the screen may cache it for a long time.
            Response.Headers.CacheControl = "public, max-age=604800";
            return PhysicalFile(file.Value.FullPath, file.Value.ContentType, enableRangeProcessing: true);
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
            return Content(QrCodeSvg.Create(text, dark), "image/svg+xml");
        }

        private bool IsLoggedIn => User.Identity?.IsAuthenticated == true;
    }
}
