using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace CreditCardRewards.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(AppDbContext context, ILogger<UserProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all user profiles
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<UserProfile>>> GetAllProfiles()
        {
            _logger.LogInformation("Fetching all user profiles");
            var profiles = await _context.UserProfiles
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Ok(profiles);
        }

        /// <summary>
        /// Get a specific user profile by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfile>> GetProfileById(Guid id)
        {
            _logger.LogInformation("Fetching user profile with ID: {ProfileId}", id);
            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile == null)
            {
                _logger.LogWarning("User profile not found with ID: {ProfileId}", id);
                return NotFound();
            }
            return Ok(profile);
        }

        /// <summary>
        /// Create a new user profile
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserProfile>> CreateProfile([FromBody] CreateUserProfileRequest request)
        {
            _logger.LogInformation("Creating new user profile: {Name} ({Email})", request.Name, request.Email);

            // Check if email already exists
            var existing = await _context.UserProfiles.FirstOrDefaultAsync(p => p.Email.ToLower() == request.Email.ToLower());
            if (existing != null)
            {
                _logger.LogWarning("User profile creation failed: Email {Email} already exists", request.Email);
                return BadRequest("A user with this email address already exists.");
            }

            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User profile created successfully with ID: {ProfileId}", profile.Id);
            return CreatedAtAction(nameof(GetProfileById), new { id = profile.Id }, profile);
        }
    }

    public class CreateUserProfileRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = null!;
    }
}
