using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;


namespace SportBuddiesServer.DTO
{
    public class User
    {
        public int UserId { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? Gender { get; set; }

        public string? IsAdmin { get; set; }

        public string? ProfileImageExtention { get; set; }

        public int? FavoriteSport { get; set; }

        public string? RoleName { get; set; }

        public int? PositionX { get; set; }
        public int? PositionY { get; set; }

        public User() { }

        public User(Models.User modelUser) 
        {
            UserId = modelUser.UserId;
            Name = modelUser.Name;
            Email = modelUser.Email;
            Password = modelUser.Password;
            Gender = modelUser.Gender;
            IsAdmin = modelUser.IsAdmin;
            ProfileImageExtention = modelUser.ProfileImageExtention;
            FavoriteSport = modelUser.FavoriteSport;
          
        }

        public Models.User GetModels()
        {
            Models.User modelsUser = new Models.User()
            {
                UserId = this.UserId,
                Name = this.Name,
                Email = this.Email,
                Password = this.Password,
                Gender = this.Gender,
                IsAdmin = this.IsAdmin,
                ProfileImageExtention = this.ProfileImageExtention,
                FavoriteSport = this.FavoriteSport,
            };
            return modelsUser;
        }
    }
}
