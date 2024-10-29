using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;


namespace SportBuddiesServer.DTO
{
    public class Message
    {
        public int MessageId { get; set; }

        public int? SenderId { get; set; }

        public int? ReceiverId { get; set; }

        public string? MessageContent { get; set; }

        public DateTime? Timestamp { get; set; }

        public Message(Models.Message modelUser)
        {
           MessageId = modelUser.MessageId;
           SenderId = modelUser.SenderId;
           ReceiverId = modelUser.ReceiverId;
           MessageContent = modelUser.MessageContent;
           Timestamp = modelUser.Timestamp;
        }

        public Models.Message GetModels()
        {
            Models.Message modelsMessage = new Models.Message()
            {
                MessageId = this.MessageId,
                SenderId = this.SenderId,
                ReceiverId = this.ReceiverId,
                MessageContent = this.MessageContent,
                Timestamp = this.Timestamp

            };

            return modelsMessage;
        }

    }
}
