using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

public partial class GameRole
{
    [Key]
    [Column("RoleID")]
    public int RoleId { get; set; }

    [Column("GameTypeID")]
    public int? GameTypeId { get; set; }

    [StringLength(255)]
    public string? Name { get; set; }

    public int? PositionX { get; set; }

    public int? PositionY { get; set; }

    [ForeignKey("GameTypeId")]
    [InverseProperty("GameRoles")]
    public virtual GameType? GameType { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<GameUser> GameUsers { get; set; } = new List<GameUser>();
}
