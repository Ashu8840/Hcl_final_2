using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Models;
using HotelBookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelBookingAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Route("api/auth")]
    public class UsersController : ControllerBase
    {
        private readonly HotelBookingDbContext _db;
        private readonly IConfiguration _configuration;

        public UsersController(HotelBookingDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        // POST: api/users/register — Register a new user
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Name, email, and password are required" });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // Check if email already exists
            var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (exists)
                return BadRequest(new { message = "Email already registered" });

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = normalizedEmail,
                PasswordHash = PasswordSecurity.HashPassword(request.Password)
            };

            user.CreatedAt = DateTime.UtcNow;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(CreateAuthResponse(user, "Registered successfully"));
        }

        // POST: api/users/login — Login
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !PasswordSecurity.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(CreateAuthResponse(user, "Login successful"));
        }

        // GET: api/users/me — Current authenticated user profile
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.Name, u.Email, u.CreatedAt })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        // GET: api/users — Get all users (admin use)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users
                .Select(u => new { u.Id, u.Name, u.Email, u.CreatedAt })
                .ToListAsync();
            return Ok(users);
        }

        private AuthResponse CreateAuthResponse(User user, string message)
        {
            var role = IsAdminEmail(user.Email) ? "Admin" : "User";
            var expiryMinutes = _configuration.GetValue<int?>("Jwt:TokenExpirationMinutes") ?? 120;
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var jwtKey = _configuration["Jwt:Key"]
                         ?? Environment.GetEnvironmentVariable("JWT__KEY")
                         ?? throw new InvalidOperationException("JWT signing key is missing.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, role)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAtUtc,
                Issuer = _configuration["Jwt:Issuer"] ?? "HotelBookingAPI",
                Audience = _configuration["Jwt:Audience"] ?? "HotelBookingUI",
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthResponse
            {
                Message = message,
                Token = tokenHandler.WriteToken(token),
                ExpiresAtUtc = expiresAtUtc,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = role
            };
        }

        private bool IsAdminEmail(string email)
        {
            var adminEmails = _configuration
                .GetSection("Jwt:AdminEmails")
                .Get<string[]>() ?? Array.Empty<string>();

            return adminEmails.Any(adminEmail =>
                string.Equals(adminEmail, email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
