using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

[Table("ChatMessages")]
public partial class ChatMessage
{
    [Key]
    [Column("MessageID")]
    public int MessageId { get; set; }

    [Column("ChatID")]
    public int ChatId { get; set; }

    [Column("SenderID")]
    public int SenderId { get; set; }

    [Column(TypeName = "ntext")]
    public string MessageContent { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? SentAt { get; set; }

    public bool? IsRead { get; set; }

    [ForeignKey("ChatId")]
    [InverseProperty("ChatMessages")]
    public virtual GameChat Chat { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("ChatMessages")]
    public virtual User Sender { get; set; } = null!;
}