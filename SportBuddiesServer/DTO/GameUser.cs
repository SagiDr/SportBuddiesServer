using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;
using Azure.Messaging;


namespace SportBuddiesServer.DTO
{
    public class GameUser
    {
        public int GameId { get; set; }

        public int RoleId { get; set; }

        public int UserId { get; set; }

        public string Team { get; set; } = "A";

        public GameUser() { }

        public GameUser(Models.GameUser modelUser)
        {
            GameId = modelUser.GameId;
            RoleId = modelUser.RoleId;
            UserId = modelUser.UserId;
            Team = modelUser.Team;
        }

        public Models.GameUser GetModels()
        {
            Models.GameUser modelsGameUser = new Models.GameUser()
            {
                GameId = this.GameId,
                RoleId = this.RoleId,
                UserId = this.UserId,
                Team = this.Team

            };

            return modelsGameUser;
        }
    }
}
