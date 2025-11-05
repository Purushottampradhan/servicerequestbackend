using Microsoft.AspNetCore.Mvc;
using ServiceRequestAPI.Auth;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace ServiceRequestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Authentication endpoints for user login")]
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenHandler _tokenHandler;
        private readonly ILogger<AuthController> _logger;

        private readonly Dictionary<string, string> _validUsers = new()
        {
            { "admin", "admin123" },
            { "user", "user123" }
        };

        public AuthController(JwtTokenHandler tokenHandler, ILogger<AuthController> logger)
        {
            _tokenHandler = tokenHandler;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/auth/login
        ///     {
        ///        "username": "admin",
        ///        "password": "admin123"
        ///     }
        ///
        /// Available test credentials:
        /// - Username: admin, Password: admin123
        /// - Username: user, Password: user123
        /// </remarks>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token if authentication successful</returns>
        /// <response code="200">Returns authentication token and user info</response>
        /// <response code="400">If username or password is missing</response>
        /// <response code="401">If credentials are invalid</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "User Login", Description = "Authenticate user with credentials and get JWT token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Username and password are required"
                    });
                }

                if (!_validUsers.TryGetValue(request.Username, out var password) || password != request.Password)
                {
                    _logger.LogWarning($"Failed login attempt for user: {request.Username}");
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                var token = _tokenHandler.GenerateToken(request.Username);
                _logger.LogInformation($"User {request.Username} logged in successfully");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    Username = request.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }
    }
}