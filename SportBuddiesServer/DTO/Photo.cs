using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SportBuddiesServer.Models;
using System.Reflection;

namespace SportBuddiesServer.DTO
{
    public class Photo
    {
        public int PhotoId { get; set; }
        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public int? GameId { get; set; }

        public Photo() { }


        public Photo(Models.Photo modelUser)
        {
            PhotoId = modelUser.PhotoId;
            ImageUrl = modelUser.ImageUrl;
            Description = modelUser.Description;
            GameId = modelUser.GameId;
        }

        public Models.Photo GetModels()
        {
            Models.Photo modelsPhoto = new Models.Photo()
            {
                PhotoId= this.PhotoId,
                ImageUrl= this.ImageUrl,
                Description= this.Description,
                GameId= this.GameId

            };

            return modelsPhoto;
        }

    }
}
