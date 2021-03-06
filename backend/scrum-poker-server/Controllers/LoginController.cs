using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using scrum_poker_server.Data;
using scrum_poker_server.DTOs.Incoming;
using scrum_poker_server.Utils.Jwt;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace scrum_poker_server.Controllers
{
    [ApiController]
    [Route("api/login")]
    public class LoginController : ControllerBase
    {
        public AppDbContext _dbContext { get; set; }

        public IConfiguration _configuration { get; set; }

        public JwtTokenGenerator JwtTokenGenerator { get; set; }

        public LoginController(AppDbContext dbContext, IConfiguration configuration, JwtTokenGenerator jwtTokenGenerator)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            JwtTokenGenerator = jwtTokenGenerator;
        }

        [Consumes("application/json")]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDTO data)
        {
            var bytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data.Password));
            string hashedPassword = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == data.Email && u.Password == hashedPassword);
            if (user == null) return Unauthorized();

            var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.UserId == user.Id);

            return Ok(new
            {
                jwtToken = JwtTokenGenerator.GenerateToken(new UserData { Email = data.Email, UserId = user.Id, Name = user.Name }),
                expiration = 29,
                name = user.Name,
                userId = user.Id,
                userRoomCode = room.Code,
                email = data.Email,
                jiraToken = user.JiraToken,
                jiraDomain = user.JiraDomain,
            });
        }
    }
}
