using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using scrum_poker_server.Data;
using scrum_poker_server.DTOs.Incoming;
using scrum_poker_server.Models;
using scrum_poker_server.Utils;
using scrum_poker_server.Utils.Jwt;
using scrum_poker_server.Utils.RoomUtils;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace scrum_poker_server.Controllers
{
    [ApiController]
    [Route("api/signup")]
    public class SignUpController : ControllerBase
    {
        public AppDbContext _dbContext { get; set; }

        public IConfiguration _configuration { get; set; }

        public JwtTokenGenerator JwtTokenGenerator { get; set; }

        public SignUpController(AppDbContext dbContext, IConfiguration configuration, JwtTokenGenerator jwtTokenGenerator)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            JwtTokenGenerator = jwtTokenGenerator;
        }

        [Consumes("application/json")]
        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] SignUpDTO data)
        {
            if (String.IsNullOrEmpty(data.Email))
            {
                var anonymousUser = new User()
                {
                    Name = data.UserName
                };

                await _dbContext.Users.AddAsync(anonymousUser);
                await _dbContext.SaveChangesAsync();

                return Ok(new { jwtToken = JwtTokenGenerator.GenerateToken(new UserData { UserId = anonymousUser.Id, Name = data.UserName }), expiration = 131399, name = data.UserName, userId = anonymousUser.Id });
            }

            bool isEmailExisted = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == data.Email) != null;
            if (isEmailExisted) return StatusCode(409, new { error = "The email is already existed" });

            // Compute hash of the password
            var bytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data.Password));
            string hashedPassword = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            var user = new User()
            {
                Email = data.Email,
                Password = hashedPassword,
                Name = data.UserName
            };

            // Create a room for user
            var roomCode = await new RoomCodeGenerator(_dbContext).Generate();

            var room = new Room
            {
                Owner = user,
                Code = roomCode,
                Name = $"{user.Name}'s room",
                Description = "Change room description here"
            };

            await _dbContext.UserRooms.AddAsync(new UserRoom
            {
                User = user,
                Room = room,
                Role = Role.host
            });

            await _dbContext.SaveChangesAsync();

            return Ok(new { jwtToken = JwtTokenGenerator.GenerateToken(new UserData { Email = data.Email, UserId = user.Id, Name = data.UserName }), email = data.Email, expiration = 29, name = data.UserName, userId = user.Id, userRoomCode = roomCode });
        }
    }
}
