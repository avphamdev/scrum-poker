﻿using System.ComponentModel.DataAnnotations.Schema;

namespace scrum_poker_server.Models
{
    public class UserRoom
    {
        [ForeignKey("User")]
        public int UserID { get; set; }

        public User User { get; set; }

        [ForeignKey("Room")]
        public int RoomId { get; set; }

        public Room Room { get; set; }

        public Role Role { get; set; }
    }

    public enum Role
    {
        host,
        player
    }
}