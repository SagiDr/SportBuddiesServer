using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;


namespace SportBuddiesServer.DTO
{
    public class User
    {
        [Required]
        public int UserId { get; set; }

        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Gender { get; set; }

        [StringLength(3)]
        public string? IsAdmin { get; set; }

        [StringLength(255)]
        public string? ProfileImageExtention { get; set; }

        public int? FavoriteSport { get; set; }
    }
}
