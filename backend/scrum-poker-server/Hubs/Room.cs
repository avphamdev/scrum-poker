using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using scrum_poker_server.HubModels;
using scrum_poker_server.HubServices;
using scrum_poker_server.Utils;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrum_poker_server.Hubs
{
    [Authorize(Policy = "AllUsers")]
    public class Room : Hub
    {
        public RoomService _roomService { get; set; }

        public Room(RoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task Combine(string roomCode, int role)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            var room = _roomService.FindRoom(roomCode);
            var userName = Context.User.FindFirst(ClaimTypes.Name).Value;
            var userId = int.Parse(Context.User.FindFirst("UserId").Value);

            if (room == null)
            {
                room = new PokingRoom(roomCode, new User(userName, userId, "standBy", (Role)role, 0), "waiting");
                _roomService.Add(room);
                var users = room.GetUsers();
                await Clients.Caller.SendAsync("joinRoom", new { users, roomState = room.State, currentStoryPoint = room.CurrentStoryPoint });
            }
            else
            {
                room.AddUser(new User(userName, int.Parse(Context.User.FindFirst("UserId").Value), "standBy", (Role)role, 0));
                var users = room.GetUsers();
                await Clients.GroupExcept(roomCode, Context.ConnectionId).SendAsync("newUserConnected", new { name = userName, id = userId, status = "standBy", point = 0, role });
                await Clients.Caller.SendAsync("joinRoom", new { users, roomState = room.State, currentStoryPoint = room.CurrentStoryPoint });
                await Clients.Caller.SendAsync("currentStoryChanged", new { id = room.CurrentStoryId });
            }
        }

        public async Task ChangeUserStatus(string roomCode, string status, int point)
        {
            var userId = int.Parse(Context.User.FindFirst("UserId").Value);
            var room = _roomService.FindRoom(roomCode);
            var user = room.Users.FirstOrDefault(u => u.Id == userId);
            user.Status = status;
            user.Point = point;

            if (room.PointsFrequency.ContainsKey(point))
            {
                room.PointsFrequency[point]++;
            }
            else
            {
                room.PointsFrequency.Add(point, 1);
            }

            await Clients.Group(roomCode).SendAsync("userStatusChanged", new { userId, status, point });
        }

        public async Task ChangeRoomState(string roomCode, string roomState)
        {
            var room = _roomService.FindRoom(roomCode);
            room.State = roomState;

            if (roomState == "revealed")
            {
                room.Users.ForEach(u => u.Status = "revealed");
                var users = room.GetUsers();
                await Clients.Group(roomCode).SendAsync("roomStateChanged", new { roomState, users });

                var highestFreq = room.PointsFrequency.Values.Max();
                var recommendedPoint = room.PointsFrequency.FirstOrDefault(item => item.Value == highestFreq).Key;
                await Clients.Group(roomCode).SendAsync("currentStoryPointChanged", new { point = recommendedPoint });
            }
            else if (roomState == "waiting")
            {
                room.Users.ForEach(u =>
                {
                    u.Status = "standBy";
                    u.Point = -1;
                });
                var users = room.GetUsers();

                await Clients.Group(roomCode).SendAsync("roomStateChanged", new { roomState, users });

                room.CurrentStoryPoint = -1;
                room.PointsFrequency.Clear();
            }
            else await Clients.Group(roomCode).SendAsync("roomStateChanged", new { roomState });
        }

        public async Task AddStory(string roomCode, int id)
        {
            await Clients.Group(roomCode).SendAsync("storyAdded", new { id });
            var room = _roomService.FindRoom(roomCode);
            room.AddStory(id);
        }

        public async Task DeleteStory(string roomCode, int id)
        {
            await Clients.Group(roomCode).SendAsync("storyDeleted", new { id });
            var room = _roomService.FindRoom(roomCode);
            room.RemoveStory(id);
        }

        public async Task UpdateStory(string roomCode, int id)
        {
            await Clients.Group(roomCode).SendAsync("storyUpdated", new { id });
        }

        public async Task ChangeCurrentStory(string roomCode, int id)
        {
            await Clients.Group(roomCode).SendAsync("currentStoryChanged", new { id });
            var room = _roomService.FindRoom(roomCode);
            room.CurrentStoryId = id;
        }

        public async Task ChangeCurrentStoryPoint(string roomCode, int point)
        {
            await Clients.Group(roomCode).SendAsync("currentStoryPointChanged", new { point });
            var room = _roomService.FindRoom(roomCode);
            room.CurrentStoryPoint = point;
        }

        public async Task RemoveFromChannel(string roomCode)
        {
            var userId = int.Parse(Context.User.FindFirst("UserId").Value);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("userLeft", new { userId });
            var room = _roomService.FindRoom(roomCode);
            room.RemoveUser(userId);
        }
    }
}
