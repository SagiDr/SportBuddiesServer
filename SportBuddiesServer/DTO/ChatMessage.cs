using System;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class ChatMessage
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string MessageContent { get; set; } = null!;
        public DateTime? SentAt { get; set; }
        public bool? IsRead { get; set; }
        public User? Sender { get; set; }

        public ChatMessage() { }

        public ChatMessage(Models.ChatMessage modelMessage)
        {
            MessageId = modelMessage.MessageId;
            ChatId = modelMessage.ChatId;
            SenderId = modelMessage.SenderId;
            MessageContent = modelMessage.MessageContent;
            SentAt = modelMessage.SentAt;
            IsRead = modelMessage.IsRead;

            if (modelMessage.Sender != null)
            {
                Sender = new User(modelMessage.Sender);
            }
        }

        public Models.ChatMessage GetModels()
        {
            return new Models.ChatMessage
            {
                MessageId = this.MessageId,
                ChatId = this.ChatId,
                SenderId = this.SenderId,
                MessageContent = this.MessageContent,
                SentAt = this.SentAt,
                IsRead = this.IsRead
            };
        }
    }
}