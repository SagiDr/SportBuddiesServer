using System;
using System.Collections.Generic;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class GameChat
    {
        public int ChatId { get; set; }
        public int GameId { get; set; }
        public string? ChatName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public GameDetails? Game { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }

        public GameChat() { }

        public GameChat(Models.GameChat modelChat)
        {
            ChatId = modelChat.ChatId;
            GameId = modelChat.GameId;
            ChatName = modelChat.ChatName;
            CreatedDate = modelChat.CreatedDate;

            if (modelChat.Game != null)
            {
                Game = new GameDetails(modelChat.Game);
            }
        }

        public Models.GameChat GetModels()
        {
            return new Models.GameChat
            {
                ChatId = this.ChatId,
                GameId = this.GameId,
                ChatName = this.ChatName,
                CreatedDate = this.CreatedDate
            };
        }
    }
}
