
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
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrollmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private bool TryGetUserID(out int userID)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userID);
        }

        private async Task<int?> GetParticipantID(int userID)
        {
            var participant = await _context.Participants
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.UserID == userID);

            return participant?.ParticipantID;
        }

        private async Task<int?> GetOrganizerID(int userID)
        {
            var organizer = await _context.Organizers
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.UserID == userID);

            return organizer?.OrganizerID;
        }

        private static EnrollmentResponse ToResponse(Enrollment enrollment)
        {
            return new EnrollmentResponse
            {
                EnrollmentID = enrollment.EnrollmentID,
                ParticipantID = enrollment.ParticipantID,
                EventID = enrollment.EventID,
                EventName = enrollment.Event.Name,
                CategoryID = enrollment.CategoryID,
                CategoryName = enrollment.Category.Name,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status
            };
        }

        // POST: api/enrollments
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<EnrollmentResponse>> CreateEnrollment(
            EnrollmentRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? participantID = await GetParticipantID(userID);

            if (participantID == null)
                return Forbid();

            var ev = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == request.EventID);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            var category = await _context.Categories
                .SingleOrDefaultAsync(c => c.CategoryID == request.CategoryID);

            if (category == null || category.EventID != request.EventID)
            {
                return BadRequest(new
                {
                    message = "Choose a category belonging to the selected event."
                });
            }

            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.ParticipantID == participantID.Value &&
                    e.EventID == request.EventID);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this event."
                });
            }

            var enrollment = new Enrollment
            {
                ParticipantID = participantID.Value,
                EventID = request.EventID,
                CategoryID = request.CategoryID,
                EnrollmentDate = DateTime.UtcNow,
                Status = "Enrolled"
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            await _context.Entry(enrollment)
                .Reference(e => e.Event).LoadAsync();

            await _context.Entry(enrollment)
                .Reference(e => e.Category).LoadAsync();

            return CreatedAtAction(
                nameof(GetMyEnrollments),
                null,
                ToResponse(enrollment));
        }

        // GET: api/enrollments/my
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<EnrollmentResponse>>>
            GetMyEnrollments()
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? participantID = await GetParticipantID(userID);

            if (participantID == null)
                return Forbid();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Where(e => e.ParticipantID == participantID.Value)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            return Ok(enrollments.Select(ToResponse));
        }

        // GET: api/enrollments/event/5
        [HttpGet("event/{eventID:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<IEnumerable<EnrollmentResponse>>>
            GetEventEnrollments(int eventID)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            var ev = await _context.Events
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EventID == eventID);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganizerID != organizerID.Value)
                return Forbid();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Where(e => e.EventID == eventID)
                .OrderBy(e => e.EnrollmentDate)
                .ToListAsync();

            return Ok(enrollments.Select(ToResponse));
        }
    }
}
