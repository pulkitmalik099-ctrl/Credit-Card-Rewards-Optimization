using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;
using CreditCardRewards.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace CreditCardRewards.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, JwtService jwt, ILogger<AuthController> logger)
        {
            _context = context;
            _jwt = jwt;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Email.ToLower() == request.Email.ToLower());

            if (existing != null)
                return Conflict("An account with this email already exists.");

            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email}", profile.Email);
            var token = _jwt.GenerateToken(profile);

            return Ok(new AuthResponse(profile, token));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Email.ToLower() == request.Email.ToLower());

            if (profile == null)
                return Unauthorized("No account found with this email.");

            // Legacy accounts (no password set) — allow login without password verification
            if (profile.PasswordHash != null && !BCrypt.Net.BCrypt.Verify(request.Password, profile.PasswordHash))
                return Unauthorized("Incorrect password.");

            _logger.LogInformation("User logged in: {Email}", profile.Email);
            var token = _jwt.GenerateToken(profile);

            return Ok(new AuthResponse(profile, token));
        }
    }

    public record RegisterRequest(
        [Required][MaxLength(100)] string Name,
        [Required][EmailAddress][MaxLength(150)] string Email,
        [Required][MinLength(8)] string Password
    );

    public record LoginRequest(
        [Required][EmailAddress] string Email,
        [Required] string Password
    );

    public record AuthResponse(Guid Id, string Name, string Email, string Token)
    {
        public AuthResponse(UserProfile p, string token) : this(p.Id, p.Name, p.Email, token) { }
    }
}
