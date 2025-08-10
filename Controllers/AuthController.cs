using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;

        public AuthController(ILogger<AuthController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            if (request == null)
                return BadRequest("Login data is required");

            // Check against all users in the system
            var user = _userService.GetByUsername(request.Username);

            if (user != null && user.Password == request.Password && user.IsActive)
            {
                var token = TokenManager.CreateToken(user.Username);

                var response = new LoginResponse
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresIn = TokenManager.ExpirySeconds,
                    Username = user.Username,
                    Role = user.Role,
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}"
                };

                _logger.LogInformation("Login successful for {Username} ({Role})", user.Username, user.Role);
                return Ok(response);
            }

            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username ?? "unknown");
            return Unauthorized(new { message = "Invalid username or password" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public static class TokenManager
    {
        private static readonly Dictionary<string, DateTime> _tokenExpiry = new();
        private const int ExpiryMinutes = 1; // 1 minutes for testing

        public static int ExpirySeconds => ExpiryMinutes * 60;

        public static string CreateToken(string username)
        {
            var token = $"token-{username}";
            _tokenExpiry[token] = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
            return token;
        }

        public static bool IsValidToken(string token)
        {
            if (!_tokenExpiry.TryGetValue(token, out var expiry))
                return false;

            if (DateTime.UtcNow > expiry)
            {
                _tokenExpiry.Remove(token);
                return false;
            }

            return true;
        }

        public static void ExtendToken(string token)
        {
            if (_tokenExpiry.ContainsKey(token))
            {
                _tokenExpiry[token] = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
            }
        }

        public static TimeSpan GetTimeRemaining(string token)
        {
            if (_tokenExpiry.TryGetValue(token, out var expiry))
                return expiry - DateTime.UtcNow;
            return TimeSpan.Zero;
        }
    }
}