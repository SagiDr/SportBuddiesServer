using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class GameType
    {
        public int IdType { get; set; }

        public string? Name { get; set; }

        public string? IconExtention { get; set; }

        public string? CourtExtention { get; set; }

        public GameType(Models.GameType modelUser)
        {
            IdType = modelUser.IdType;
            Name = modelUser.Name;
            IconExtention = modelUser.IconExtention;
            CourtExtention = modelUser.CourtExtention;
        }

        public Models.GameType GetModels()
        {
            Models.GameType modelsGameType = new Models.GameType()
            {
                IdType = this.IdType,
                Name = this.Name,
                IconExtention = this.IconExtention,
                CourtExtention = this.CourtExtention
            };

            return modelsGameType;
        }





        //// Optionally, include collections if needed for specific operations
        //public ICollection<int>? GameDetailIds { get; set; } = new List<int>();
        //public ICollection<int>? GameRoleIds { get; set; } = new List<int>();
        //public ICollection<int>? UserIds { get; set; } = new List<int>();
    }
}
