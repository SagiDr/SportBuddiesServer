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

        public GameDetails() { }

        public GameDetails(Models.GameDetails modelUser)
        {
            GameId = modelUser.GameId;
            GameName = modelUser.GameName;
            Date = modelUser.Date;
            Time = modelUser.Time;
            Location = modelUser.Location;
            GameType = modelUser.GameType;
            State = modelUser.State;
            Score = modelUser.Score;
            Notes = modelUser.Notes;
            Competitive = modelUser.Competitive;
            Link = modelUser.Link;
            LocationLength = modelUser.LocationLength;
            LocationWidth = modelUser.LocationWidth;
            CreatorId = modelUser.CreatorId;
        }

        public Models.GameDetails GetModels()
        {
            Models.GameDetails modelsGameDetails = new Models.GameDetails()
            {
                GameId = this.GameId,
                GameName = this.GameName,
                Date = this.Date,
                Time = this.Time,
                Location = this.Location,
                GameType= this.GameType,
                State = this.State,
                Score = this.Score,
                Notes = this.Notes,
                Competitive = this.Competitive,
                Link= this.Link,
                LocationWidth = this.LocationWidth,
                LocationLength = this.LocationLength,
                CreatorId = this.CreatorId,
            };
            return modelsGameDetails;
        }


        //// Optionally include collections if needed for specific operations
        //    public ICollection<int>? GameUserIds { get; set; } = new List<int>();
        //    public ICollection<int>? PhotoIds { get; set; } = new List<int>();
        //}
    }
}
