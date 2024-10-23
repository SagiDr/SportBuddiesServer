using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;

namespace SportBuddiesServer.DTO
{
    public class Photo
    {
        [Required]
        public int PhotoId { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public int? GameId { get; set; }
    }
}
