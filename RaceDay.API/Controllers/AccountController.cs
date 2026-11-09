
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(
            RaceDayContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // POST: api/account/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber?.Trim(),
                Role = request.Role,
                ProfileImageUrl = null
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(user, request.Password);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (user.Role == "Organizer")
                {
                    _context.Organizers.Add(new Organizer
                    {
                        UserID = user.UserID
                    });
                }
                else
                {
                    _context.Participants.Add(new Participant
                    {
                        UserID = user.UserID
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(201, new
                {
                    message = "Registration successful. Please log in.",
                    userID = user.UserID,
                    role = user.Role
                });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = "The account could not be created. Check whether the email is already registered."
                });
            }
        }

        // POST: api/account/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var verification = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserID.ToString()),

                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            return Ok(new
            {
                message = "Login successful.",
                userID = user.UserID,
                role = user.Role
            });
        }

        // POST: api/account/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { message = "Logout successful." });
        }

        // GET: api/account/profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponse>> GetProfile()
        {
            string? userIDText =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIDText, out int userID))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserID == userID);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(ToProfileResponse(user));
        }

        // PUT: api/account/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponse>> UpdateProfile(
            UpdateProfileRequest request)
        {
            string? userIDText =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIDText, out int userID))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.UserID == userID);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.ProfileImageUrl = request.ProfileImageUrl;

            await _context.SaveChangesAsync();

            return Ok(ToProfileResponse(user));
        }

        private static UserProfileResponse ToProfileResponse(User user)
        {
            return new UserProfileResponse
            {
                UserID = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }
    }
}
