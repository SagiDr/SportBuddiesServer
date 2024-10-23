using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;


namespace SportBuddiesServer.DTO
{
    public class GameDetails
    {
        [Required]
        public int GameId { get; set; }

        [StringLength(255)]
        public string? GameName { get; set; }

        public DateOnly? Date { get; set; }

        public TimeOnly? Time { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        public int? GameType { get; set; }

        [StringLength(10)]
        public string? State { get; set; }

        [StringLength(50)]
        public string? Score { get; set; }

        public string? Notes { get; set; }

        [StringLength(255)]
        public string? Competitive { get; set; }

        [StringLength(255)]
        public string? Link { get; set; }

        public decimal? LocationLength { get; set; }

        public decimal? LocationWidth { get; set; }

        public int? CreatorId { get; set; }

        // Optionally include collections if needed for specific operations
        public ICollection<int>? GameUserIds { get; set; } = new List<int>();
        public ICollection<int>? PhotoIds { get; set; } = new List<int>();
    }
}
