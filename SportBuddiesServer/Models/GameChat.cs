// Models/GameChat.cs
using Microsoft.EntityFrameworkCore;
using SportBuddiesServer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportBuddiesServer.Models;

[Table("GameChat")]
public partial class GameChat
{
    [Key]
    [Column("ChatID")]
    public int ChatId { get; set; }

    [Column("GameID")]
    public int GameId { get; set; }

    [StringLength(255)]
    public string? ChatName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("GameId")]
    [InverseProperty("GameChats")]
    public virtual GameDetail Game { get; set; } = null!;

    [InverseProperty("Chat")]
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}