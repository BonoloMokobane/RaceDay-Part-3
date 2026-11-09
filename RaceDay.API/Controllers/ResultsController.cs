
using System.Globalization;
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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
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

        private static ResultResponse ToResponse(Result result)
        {
            return new ResultResponse
            {
                ResultID = result.ResultID,
                EnrollmentID = result.EnrollmentID,
                EventID = result.Enrollment.EventID,
                EventName = result.Enrollment.Event.Name,
                ParticipantName =
                    result.Enrollment.Participant.User.FirstName + " " +
                    result.Enrollment.Participant.User.LastName,
                FinishTime = result.FinishTime,
                FinishingPosition = result.FinishingPosition
            };
        }

        private static bool TryParseFinishTime(
            string value, out TimeSpan finishTime)
        {
            return TimeSpan.TryParse(
                       value,
                       CultureInfo.InvariantCulture,
                       out finishTime)
                   && finishTime > TimeSpan.Zero;
        }

        private async Task<Result?> LoadResult(int resultID)
        {
            return await _context.Results
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Event)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                        .ThenInclude(p => p.User)
                .SingleOrDefaultAsync(r => r.ResultID == resultID);
        }

        // POST: api/results/enrollment/5
        [HttpPost("enrollment/{enrollmentID:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<ResultResponse>> CreateResult(
            int enrollmentID, ResultRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            if (!TryParseFinishTime(request.FinishTime, out TimeSpan finishTime))
            {
                return BadRequest(new
                {
                    message = "Enter a valid positive finish time, such as 01:25:10."
                });
            }

            if (request.FinishingPosition == null ||
                request.FinishingPosition <= 0)
            {
                return BadRequest(new
                {
                    message = "Finishing position must be greater than zero."
                });
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Event)
                .Include(e => e.Participant)
                    .ThenInclude(p => p.User)
                .SingleOrDefaultAsync(e => e.EnrollmentID == enrollmentID);

            if (enrollment == null)
                return NotFound(new { message = "Enrollment not found." });

            if (enrollment.Event.OrganizerID != organizerID.Value)
                return Forbid();

            bool resultExists = await _context.Results
                .AnyAsync(r => r.EnrollmentID == enrollmentID);

            if (resultExists)
            {
                return Conflict(new
                {
                    message = "A result already exists for this enrollment. Use the update endpoint."
                });
            }

            var result = new Result
            {
                EnrollmentID = enrollmentID,
                FinishTime = finishTime,
                FinishingPosition = request.FinishingPosition.Value,
                Enrollment = enrollment
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                ToResponse(result));
        }

        // PUT: api/results/enrollment/5
        [HttpPut("enrollment/{enrollmentID:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<ResultResponse>> UpdateResult(
            int enrollmentID, ResultRequest request)
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? organizerID = await GetOrganizerID(userID);

            if (organizerID == null)
                return Forbid();

            if (!TryParseFinishTime(request.FinishTime, out TimeSpan finishTime))
            {
                return BadRequest(new
                {
                    message = "Enter a valid positive finish time, such as 01:25:10."
                });
            }

            if (request.FinishingPosition == null ||
                request.FinishingPosition <= 0)
            {
                return BadRequest(new
                {
                    message = "Finishing position must be greater than zero."
                });
            }

            var result = await _context.Results
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Event)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                        .ThenInclude(p => p.User)
                .SingleOrDefaultAsync(r => r.EnrollmentID == enrollmentID);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            if (result.Enrollment.Event.OrganizerID != organizerID.Value)
                return Forbid();

            result.FinishTime = finishTime;
            result.FinishingPosition = request.FinishingPosition.Value;

            await _context.SaveChangesAsync();

            return Ok(ToResponse(result));
        }

        // GET: api/results/my
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<ResultResponse>>> GetMyResults()
        {
            if (!TryGetUserID(out int userID))
                return Unauthorized();

            int? participantID = await GetParticipantID(userID);

            if (participantID == null)
                return Forbid();

            var results = await _context.Results
                .AsNoTracking()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Event)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                        .ThenInclude(p => p.User)
                .Where(r => r.Enrollment.ParticipantID == participantID.Value)
                .OrderBy(r => r.FinishingPosition)
                .ToListAsync();

            return Ok(results.Select(ToResponse));
        }

        // GET: api/results/event/5
        [HttpGet("event/{eventID:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<IEnumerable<ResultResponse>>> GetEventResults(
            int eventID)
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

            var results = await _context.Results
                .AsNoTracking()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Event)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                        .ThenInclude(p => p.User)
                .Where(r => r.Enrollment.EventID == eventID)
                .OrderBy(r => r.FinishingPosition)
                .ToListAsync();

            return Ok(results.Select(ToResponse));
        }
    }
}
