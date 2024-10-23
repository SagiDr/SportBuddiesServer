using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class GameRole
    {
        [Required]
        public int RoleId { get; set; }

        public int? GameTypeId { get; set; }

        [StringLength(255)]
        public string? Name { get; set; }

        public int? PositionX { get; set; }

        public int? PositionY { get; set; }

        // Optionally, include a collection of GameUser IDs if needed
        public ICollection<int>? GameUserIds { get; set; } = new List<int>();
    }
}
