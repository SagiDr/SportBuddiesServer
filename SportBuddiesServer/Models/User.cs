using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

[Table("User")]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Password { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Gender { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string? IsAdmin { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? ProfileImageExtention { get; set; }

    public int? FavoriteSport { get; set; }


    [ForeignKey("FavoriteSport")]
    [InverseProperty("Users")]
    public virtual GameType? FavoriteSportNavigation { get; set; }

    [InverseProperty("Creator")]
    public virtual ICollection<GameDetail> GameDetails { get; set; } = new List<GameDetail>();

    [InverseProperty("User")]
    public virtual ICollection<GameUser> GameUsers { get; set; } = new List<GameUser>();

    // Add this new property for chat messages
    [InverseProperty("Sender")]
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
