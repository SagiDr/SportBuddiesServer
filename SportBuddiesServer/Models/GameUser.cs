using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

[PrimaryKey("GameId", "RoleId", "UserId")]
public partial class GameUser
{
    [Key]
    public int GameId { get; set; }

    [Key]
    public int RoleId { get; set; }

    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(1)]
    public string Team { get; set; } = "A";

    [ForeignKey("GameId")]
    [InverseProperty("GameUsers")]
    public virtual GameDetail Game { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("GameUsers")]
    public virtual GameRole Role { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("GameUsers")]
    public virtual User User { get; set; } = null!;
}
