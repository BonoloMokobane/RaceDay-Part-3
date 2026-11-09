
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
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
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

        private static EventResponse ToResponse(Event ev)
        {
            return new EventResponse
            {
                EventID = ev.EventID,
                OrganizerID = ev.OrganizerID,
                Name = ev.Name,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Distance = ev.Distance,
                EventType = ev.EventType,
                BannerImageUrl = ev.BannerImageUrl
            };
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events.Select(ToResponse));
        }

        // GET: api/events/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventResponse>> GetEvent(int id)
        {
            var ev = await _context.Events
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            return Ok(ToResponse(ev));
        }

        // POST: api/events
        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<EventResponse>> CreateEvent(
            EventRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var ev = new Event
            {
                OrganizerID = organizerID.Value,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                EventDate = request.EventDate,
                Location = request.Location.Trim(),
                Distance = request.Distance,
                EventType = request.EventType,
                BannerImageUrl = request.BannerImageUrl
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = ev.EventID },
                ToResponse(ev));
        }

        // PUT: api/events/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<EventResponse>> UpdateEvent(
            int id, EventRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var ev = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganizerID != organizerID.Value)
                return Forbid();

            ev.Name = request.Name.Trim();
            ev.Description = request.Description?.Trim();
            ev.EventDate = request.EventDate;
            ev.Location = request.Location.Trim();
            ev.Distance = request.Distance;
            ev.EventType = request.EventType;
            ev.BannerImageUrl = request.BannerImageUrl;

            await _context.SaveChangesAsync();

            return Ok(ToResponse(ev));
        }

        // DELETE: api/events/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var ev = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganizerID != organizerID.Value)
                return Forbid();

            bool hasCategories = await _context.Categories
                .AnyAsync(c => c.EventID == id);

            bool hasEnrollments = await _context.Enrollments
                .AnyAsync(e => e.EventID == id);

            if (hasCategories || hasEnrollments)
            {
                return Conflict(new
                {
                    message = "Remove the event's categories and enrollments before deleting this event."
                });
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

