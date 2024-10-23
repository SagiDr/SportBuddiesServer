using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

[Table("GameType")]
public partial class GameType
{
    [Key]
    public int IdType { get; set; }

    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(255)]
    public string? IconExtention { get; set; }

    [StringLength(255)]
    public string? CourtExtention { get; set; }

    [InverseProperty("GameTypeNavigation")]
    public virtual ICollection<GameDetails> GameDetails { get; set; } = new List<GameDetails>();

    [InverseProperty("GameType")]
    public virtual ICollection<GameRole> GameRoles { get; set; } = new List<GameRole>();

    [InverseProperty("FavoriteSportNavigation")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
