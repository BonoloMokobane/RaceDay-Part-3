
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Organizer,Participant")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        private bool TryGetUserID(out int userID)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userID);
        }

        private async Task<int?> GetOrganizerID(int userID)
        {
            var organizer = await _context.Organizers
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.UserID == userID);

            return organizer?.OrganizerID;
        }

        private static CategoryResponse ToResponse(Category category)
        {
            return new CategoryResponse
            {
                CategoryID = category.CategoryID,
                EventID = category.EventID,
                Name = category.Name,
                Description = category.Description
            };
        }

        // GET: api/categories/event/5
        [HttpGet("event/{eventID:int}")]
        public async Task<ActionResult<IEnumerable<CategoryResponse>>>
            GetCategoriesForEvent(int eventID)
        {
            bool eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventID);

            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventID == eventID)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories.Select(ToResponse));
        }

        // GET: api/categories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            return Ok(ToResponse(category));
        }

        // POST: api/categories/event/5
        [HttpPost("event/{eventID:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<CategoryResponse>> CreateCategory(
            int eventID, CategoryRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var ev = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == eventID);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganizerID != organizerID.Value)
                return Forbid();

            var category = new Category
            {
                EventID = eventID,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryID },
                ToResponse(category));
        }

        // PUT: api/categories/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<CategoryResponse>> UpdateCategory(
            int id, CategoryRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var category = await _context.Categories
                .SingleOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            var ev = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == category.EventID);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganizerID != organizerID.Value)
                return Forbid();

            category.Name = request.Name.Trim();
            category.Description = request.Description?.Trim();

            await _context.SaveChangesAsync();

            return Ok(ToResponse(category));
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var category = await _context.Categories
                .SingleOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            var ev = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == category.EventID);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganizerID != organizerID.Value)
                return Forbid();

            bool hasEnrollments = await _context.Enrollments
                .AnyAsync(e => e.CategoryID == id);

            if (hasEnrollments)
            {
                return Conflict(new
                {
                    message = "This category has enrollments and cannot be deleted."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

