
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageUploadController : ControllerBase
    {
        private readonly AzureBlobStorageService _blobStorageService;
        private readonly IConfiguration _configuration;
        private readonly RaceDayContext _context;

        public ImageUploadController(
            AzureBlobStorageService blobStorageService,
            IConfiguration configuration,
            RaceDayContext context)
        {
            _blobStorageService = blobStorageService;
            _configuration = configuration;
            _context = context;
        }

        // POST: api/ImageUpload/profile
        [HttpPost("profile")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfileImage(
            IFormFile file)
        {
            try
            {
                string? userIDText =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIDText, out int userID))
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == userID);

                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var containerName =
                    _configuration["AzureBlobStorage:ProfileContainer"];

                if (string.IsNullOrWhiteSpace(containerName))
                {
                    return StatusCode(500,
                        "Profile image container is not configured.");
                }

                var imageUrl =
                    await _blobStorageService.UploadImageAsync(
                        file, containerName);

                user.ProfileImageUrl = imageUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Profile image uploaded and saved successfully.",
                    imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500,
                    "An error occurred while uploading the profile image.");
            }
        }

        // POST: api/ImageUpload/event-banner/{eventId}
        [HttpPost("event-banner/{eventId:int}")]
        [Authorize(Roles = "Organizer")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEventBanner(
            int eventId,
            IFormFile file)
        {
            try
            {
                string? userIDText =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIDText, out int userID))
                {
                    return Unauthorized();
                }

                var raceEvent = await _context.Events
                    .FirstOrDefaultAsync(e =>
                        e.EventID == eventId &&
                        e.Organizer.UserID == userID);

                if (raceEvent == null)
                {
                    return NotFound(
                        "Event not found or you do not own this event.");
                }

                var containerName =
                    _configuration["AzureBlobStorage:EventContainer"];

                if (string.IsNullOrWhiteSpace(containerName))
                {
                    return StatusCode(500,
                        "Event banner container is not configured.");
                }

                var imageUrl =
                    await _blobStorageService.UploadImageAsync(
                        file, containerName);

                raceEvent.BannerImageUrl = imageUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Event banner uploaded and saved successfully.",
                    eventId = raceEvent.EventID,
                    imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500,
                    "An error occurred while uploading the event banner.");
            }
        }
    }
}
