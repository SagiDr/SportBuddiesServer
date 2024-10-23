using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class GameType
    {
        [Required]
        public int IdType { get; set; }

        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(255)]
        public string? IconExtention { get; set; }

        [StringLength(255)]
        public string? CourtExtention { get; set; }

        // Optionally, include collections if needed for specific operations
        public ICollection<int>? GameDetailIds { get; set; } = new List<int>();
        public ICollection<int>? GameRoleIds { get; set; } = new List<int>();
        public ICollection<int>? UserIds { get; set; } = new List<int>();

    }
}
