using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using scrum_poker_server.Data;
using scrum_poker_server.DTOs;
using scrum_poker_server.HubServices;
using scrum_poker_server.Models;
using scrum_poker_server.Utils;
using scrum_poker_server.Utils.RoomUtils;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrum_poker_server.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomController : ControllerBase
    {
        public AppDbContext _dbContext { get; set; }

        public RoomService _roomService { get; set; }

        public RoomController(AppDbContext dbContext, RoomService roomService)
        {
            _dbContext = dbContext;
            _roomService = roomService;
        }

        [Authorize(Policy = "OfficialUsers")]
        [Consumes("application/json")]
        [HttpPost, Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateRoomDTO data)
        {
            var roomCode = await new RoomCodeGenerator(_dbContext).Generate();

            var user = _dbContext.Users.FirstOrDefault(u => u.Email == HttpContext.User.FindFirst(ClaimTypes.Email).Value);

            var room = new Room
            {
                Owner = user,
                Code = roomCode,
                Name = data.RoomName,
                Description = data.Description
            };

            await _dbContext.UserRooms.AddAsync(new UserRoom
            {
                User = user,
                Room = room,
                Role = Role.host
            });

            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new { code = roomCode, roomId = room.Id });
        }

        [Authorize(Policy = "AllUsers")]
        [HttpGet, Route("{id}/stories")]
        public async Task<IActionResult> GetStories(int id)
        {
            var room = await _dbContext.Rooms.Include(r => r.Stories).ThenInclude(s => s.SubmittedPointByUsers).ThenInclude(s => s.User).AsSplitQuery().FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound(new { error = "The room doesn't exist" });

            var userRoom = await _dbContext.UserRooms.
                FirstOrDefaultAsync(ur => ur.RoomId == room.Id && ur.UserID.ToString() == HttpContext.User.FindFirst("UserId").Value);

            if (userRoom == null) return Forbid();

            var stories = new List<StoryDTO>();
            room.Stories.ToList().ForEach(s =>
            {
                var submittedPointByUsers = new List<DTOs.SubmittedPointByUser>();

                if (s.SubmittedPointByUsers != null)
                {
                    s.SubmittedPointByUsers.ToList().ForEach(s =>
                    {
                        submittedPointByUsers.Add(new DTOs.SubmittedPointByUser
                        {
                            UserId = s.UserId,
                            UserName = s.User.Name,
                            Point = s.Point
                        });
                    });
                }

                stories.Add(new StoryDTO
                {
                    Id = s.Id,
                    Title = s.Title,
                    Content = s.Content,
                    Assignee = s.Assignee,
                    Point = s.Point,
                    IsJiraStory = s.IsJiraStory,
                    JiraIssueId = s.JiraIssueId,
                    SubmittedPointByUsers = submittedPointByUsers,
                }
                );
            });

            return Ok(new { stories });
        }

        [Authorize(Policy = "AllUsers")]
        [Consumes("application/json")]
        [HttpPost, Route("join")]
        public async Task<IActionResult> Join([FromBody] JoinRoomDTO data)
        {
            var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Code == data.RoomCode);
            if (room == null) return NotFound(new { error = "The room doesn't exist" });

            var userId = HttpContext.User.FindFirst("UserId").Value;

            var userRoom = await _dbContext.UserRooms.FirstOrDefaultAsync(ur => ur.Room.Code == data.RoomCode && ur.User.Id.ToString() == userId);

            if (userRoom != null) return Ok(new { roomId = room.Id, roomCode = data.RoomCode, roomName = room.Name, description = room.Description, role = userRoom.Role, jiraDomain = room.JiraDomain });

            await _dbContext.UserRooms.AddAsync(new UserRoom
            {
                User = await _dbContext.Users.FirstAsync(u => u.Id.ToString() == userId),
                Room = room,
                Role = Role.player
            });

            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new { roomId = room.Id, roomCode = data.RoomCode, roomName = room.Name, description = room.Description, role = Role.player, jiraDomain = room.JiraDomain });
        }

        // This API is used to check the availability of a room (valid room code, full people)
        [Authorize(Policy = "OfficialUsers")]
        [HttpGet, Route("checkroom/{roomCode}")]
        public async Task<IActionResult> CheckRoom(string roomCode)
        {
            var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Code == roomCode);
            var userClaim = HttpContext.User.FindFirst("UserId");
            var userId = int.Parse(userClaim.Value);

            if (room == null)
            {
                return StatusCode(404);
            }
            else if (_roomService.FindRoom(roomCode) == null)
            {
                return Ok();
            }
            else if (_roomService.FindRoom(roomCode).Users.Count >= 12)
            {
                return StatusCode(403);
            }
            else if (_roomService.FindRoom(roomCode).Users.Find(u => u.Id == userId) != null)
            {
                return StatusCode(409);
            }

            return Ok();
        }
    }
}
