using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Minimal_chat_application.Context;
using Minimal_chat_application.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;



namespace Minimal_chat_application.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;

        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel userRegisterModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            var user = new User
            {
                Email = userRegisterModel.Email,
                UserName = userRegisterModel.FirstName,
                FirstName = userRegisterModel.FirstName,
                LastName = userRegisterModel.LastName
            };

            var result = await _userManager.CreateAsync(user, userRegisterModel.Password);

            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    userId = user.Id,
                    firstName = user.FirstName, 
                    lastName = user.LastName,
                    email = user.Email
                });

            }
            else
            {
                return Conflict(new { error = "User already registered with this email" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { error = "User not found" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var token = GenerateJwtToken(user);
                var profile = new
                {
                    id = user.Id,
                    name = user.UserName,
                    email = user.Email
                };
                return Ok(new { token, profile });
            }
            else
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }
        }

        [HttpGet("GetUsers")]
        [Authorize]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Select(u => new User
                {
                    Id = u.Id,
                    FirstName = u.FirstName + " " + u.LastName,
                    Email = u.Email
                })
                .ToList();

            if (users.Count == 0)
            {
                return NotFound(new { error = "No users found" });
            }

            return Ok(new { users });
        }

        [HttpPost("SendMessages")]
        [Authorize]
        
        public async Task<IActionResult> SendMessage([FromBody] SendMessageModel sendMessageModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(senderId))
            {
                return Unauthorized(new { error = "Unauthorized access" });
            }

            // Check if the receiver exists
            var receiver = await _userManager.FindByIdAsync(sendMessageModel.ReceiverId);
            if (receiver == null)
            {
                return BadRequest(new { error = "Receiver user not found" });
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = sendMessageModel.ReceiverId,
                Content = sendMessageModel.Content,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                messageId = message.Id,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                timestamp = message.Timestamp
            });
        }

        //Generate jwt token
        private string GenerateJwtToken(IdentityUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
