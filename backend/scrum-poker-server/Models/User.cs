﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrum_poker_server.Models
{
    public class User
    {
        public int Id { get; set; }

        [MaxLength(25)]
        public string Name { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Password { get; set; }

        public string JiraToken { get; set; }

        public string JiraDomain { get; set; }

        public string JiraEmail { get; set; }

        public ICollection<UserRoom> UserRooms { get; set; }
    }
}
