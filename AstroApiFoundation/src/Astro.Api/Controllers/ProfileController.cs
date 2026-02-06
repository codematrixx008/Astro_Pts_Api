using Astro.Application.Security;
using Astro.Domain.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers
{
    [ApiController]
    [Route("profile")] // ✅ REQUIRED
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IProfileRepository _repo;

        public ProfileController(
            IWebHostEnvironment env,
            IProfileRepository repo)
        {
            _env = env;
            _repo = repo;
        }

        [HttpGet("me/profile-image")]
        public async Task<IActionResult> GetMyProfileImage()
        {
            var userId = User.GetUserId();

            var imagePath = await _repo.GetProfileImageAsync(userId);

            if (string.IsNullOrWhiteSpace(imagePath))
                return Ok(new { imageUrl = (string?)null });

            var fullUrl = $"{Request.Scheme}://{Request.Host}{imagePath}";

            return Ok(new { imageUrl = fullUrl });
        }

        [HttpPost("me/profile-image")]
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var userId = User.GetUserId();

            var webRoot = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var uploadsFolder = Path.Combine(webRoot, "uploads", "profile");

            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"user_{userId}_{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var imagePath = $"/uploads/profile/{fileName}";
            var savedPath = await _repo.UpdateProfileImageAsync(userId, imagePath);

            return Ok(new ProfileImageResult
            {
                ImageUrl = $"{Request.Scheme}://{Request.Host}{savedPath}"
            });
        }

    }
}
