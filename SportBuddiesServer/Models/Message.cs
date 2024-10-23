using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

public partial class Message
{
    [Key]
    [Column("MessageID")]
    public int MessageId { get; set; }

    [Column("SenderID")]
    public int? SenderId { get; set; }

    [Column("ReceiverID")]
    public int? ReceiverId { get; set; }

    [Column(TypeName = "text")]
    public string? MessageContent { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Timestamp { get; set; }

    [ForeignKey("ReceiverId")]
    [InverseProperty("MessageReceivers")]
    public virtual User? Receiver { get; set; }

    [ForeignKey("SenderId")]
    [InverseProperty("MessageSenders")]
    public virtual User? Sender { get; set; }
}
