using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

public partial class GameDetail
{
    [Key]
    [Column("GameID")]
    public int GameId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? GameName { get; set; }

    public DateOnly? Date { get; set; }

    public TimeOnly? Time { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Location { get; set; }

    public int? GameType { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? State { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Score { get; set; }

    [Column(TypeName = "text")]
    public string? Notes { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Competitive { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Link { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? LocationLength { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? LocationWidth { get; set; }

    public int? CreatorId { get; set; }

    [ForeignKey("CreatorId")]
    [InverseProperty("GameDetails")]
    public virtual User? Creator { get; set; }

    [ForeignKey("GameType")]
    [InverseProperty("GameDetails")]
    public virtual GameType? GameTypeNavigation { get; set; }

    [InverseProperty("Game")]
    public virtual ICollection<GameUser> GameUsers { get; set; } = new List<GameUser>();

    [InverseProperty("Game")]
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
