using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class GameRole
    {
        public int RoleId { get; set; }

        public int? GameTypeId { get; set; }

        public string? Name { get; set; }

        public int? PositionX { get; set; }

        public int? PositionY { get; set; }

        public GameRole(Models.GameRole modelUser)
        {
            RoleId = modelUser.RoleId;
            GameTypeId = modelUser.GameTypeId;
            Name = modelUser.Name;
            PositionX = modelUser.PositionX;
            PositionY = modelUser.PositionY;
        }

        public Models.GameRole GetModels()
        {
            Models.GameRole modelsGameRole = new Models.GameRole()
            {
               RoleId = this.RoleId,
               GameTypeId = this.GameTypeId,
               Name = this.Name,
               PositionX = this.PositionX,
               PositionY = this.PositionY,
            };

            return modelsGameRole;
        }
        //// Optionally, include a collection of GameUser IDs if needed
        //public ICollection<int>? GameUserIds { get; set; } = new List<int>();
    }
}
