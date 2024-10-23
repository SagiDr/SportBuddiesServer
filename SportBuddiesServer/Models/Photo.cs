using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

[Table("Photo")]
public partial class Photo
{
    [Key]
    [Column("PhotoID")]
    public int PhotoId { get; set; }

    [Column("ImageURL")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ImageUrl { get; set; }

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    [Column("GameID")]
    public int? GameId { get; set; }

    [ForeignKey("GameId")]
    [InverseProperty("Photos")]
    public virtual GameDetails? Game { get; set; }
}
