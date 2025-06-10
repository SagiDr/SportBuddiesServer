using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class GameDetails
    {
        public int GameId { get; set; }

        public string? GameName { get; set; }

        public DateOnly? Date { get; set; }

        public TimeOnly? Time { get; set; }

        public string? Location { get; set; }

        public int? GameType { get; set; }

        public string? State { get; set; }

        public string? Score { get; set; }

        public string? Notes { get; set; }

        public string? Competitive { get; set; }

        public string? Link { get; set; }

        public decimal? LocationLength { get; set; }

        public decimal? LocationWidth { get; set; }

        public int? CreatorId { get; set; }
        public User? Creator { get; set; }

        public GameDetails() { }

        public GameDetails(Models.GameDetail modelGameDetail)
        {
            GameId = modelGameDetail.GameId;
            GameName = modelGameDetail.GameName;
            Date = modelGameDetail.Date;
            Time = modelGameDetail.Time;
            Location = modelGameDetail.Location;
            GameType = modelGameDetail.GameType;
            State = modelGameDetail.State;
            Score = modelGameDetail.Score;
            Notes = modelGameDetail.Notes;
            Competitive = modelGameDetail.Competitive;
            Link = modelGameDetail.Link;
            LocationLength = modelGameDetail.LocationLength;
            LocationWidth = modelGameDetail.LocationWidth;
            CreatorId = modelGameDetail.CreatorId;
            if (modelGameDetail.Creator != null)
                Creator = new User(modelGameDetail.Creator);
        }

        public Models.GameDetail GetModels()
        {
            Models.GameDetail modelsGameDetails = new Models.GameDetail()
            {
                GameId = this.GameId,
                GameName = this.GameName,
                Date = this.Date,
                Time = this.Time,
                Location = this.Location,
                GameType = this.GameType,
                State = this.State,
                Score = this.Score,
                Notes = this.Notes,
                Competitive = this.Competitive,
                Link = this.Link,
                LocationWidth = this.LocationWidth,
                LocationLength = this.LocationLength,
                CreatorId = this.CreatorId,
            };
            return modelsGameDetails;
        }
    }
}