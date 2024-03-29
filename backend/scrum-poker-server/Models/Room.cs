﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrum_poker_server.Models
{
  public class Room
  {
    public int Id { get; set; }

    [Column(TypeName = "char(6)")]
    public string Code { get; set; }

    [MaxLength(30)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }

    public string JiraDomain { get; set; }

    [ForeignKey("Owner")]
    public int UserId { get; set; }

    public User Owner { get; set; }

    public ICollection<UserRoom> UserRooms { get; set; }

    public ICollection<Story> Stories { get; set; }
  }
}
