using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportBuddiesServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
namespace SportBuddiesServer.Controllers
{
    [Route("api")]
    [ApiController]
    public class SportBuddiesAPIController : ControllerBase
    {
        //a variable to hold a reference to the db context!
        private SportBuddiesDbContext context;
        //a variable that hold a reference to web hosting interface (that provide information like the folder on which the server runs etc...)
        private IWebHostEnvironment webHostEnvironment;
        //Use dependency injection to get the db context and web host into the constructor
        public SportBuddiesAPIController(SportBuddiesDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.webHostEnvironment = env;
        }

        // New method to check game capacity limits based on game type
        private bool HasGameReachedCapacity(int gameId)
        {
            var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
            if (game == null)
                return false;

            var currentPlayerCount = context.GameUsers.Count(gu => gu.GameId == gameId);

            // Get maximum player count based on game type
            int maxPlayers;
            switch (game.GameType)
            {
                case 1: // Basketball
                    maxPlayers = 5;
                    break;
                case 2: // Soccer
                    maxPlayers = 11; // Changed to 11 - standard soccer team size
                    break;
                case 3: // Volleyball
                    maxPlayers = 6; // Changed to 6 - standard volleyball team size
                    break;
                default:
                    maxPlayers = 10; // Default maximum
                    break;
            }

            // Check if current player count has reached the maximum
            return currentPlayerCount >= maxPlayers;
        }

        // New method to get available roles for a game
        private List<GameRole> GetAvailableRoles(int gameId, int gameTypeId)
        {
            // Get all roles for this game type
            var allRoles = context.GameRoles.Where(r => r.GameTypeId == gameTypeId).ToList();

            // Get roles that are already assigned in this game
            var assignedRoleIds = context.GameUsers
                .Where(gu => gu.GameId == gameId)
                .Select(gu => gu.RoleId)
                .ToList();

            // Return roles that are not yet assigned
            return allRoles.Where(r => !assignedRoleIds.Contains(r.RoleId)).ToList();
        }



        [HttpPost("login")]
        public IActionResult Login([FromBody] DTO.LoginInfo loginDto)
        {
            try
            {
                HttpContext.Session.Clear(); //Logout any previous login attempt

                //Get model user class from DB with matching email. 
                Models.User? modelsUser = context.GetUser(loginDto.Email);

                //Check if user exist for this email and if password match, if not return Access Denied (Error 403) 
                if (modelsUser == null || modelsUser.Password != loginDto.Password)
                {
                    return Unauthorized();
                }

                //Login suceed! now mark login in session memory!
                HttpContext.Session.SetString("loggedInUser", modelsUser.Email);

                DTO.User dtoUser = new DTO.User(modelsUser);
                dtoUser.ProfileImageExtention = GetProfileImageVirtualPath(dtoUser.UserId);
                return Ok(dtoUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] DTO.User userDto)
        {
            try
            {
                HttpContext.Session.Clear(); //Logout any previous login attempt

                //Create model user class
                Models.User modelsUser = userDto.GetModels();

                // Set default profile image path if not provided
                if (string.IsNullOrEmpty(modelsUser.ProfileImageExtention))
                {
                    modelsUser.ProfileImageExtention = "/profileImages/default.png";
                }

                context.Users.Add(modelsUser);
                context.SaveChanges();

                //User was added!
                DTO.User dtoUser = new DTO.User(modelsUser);
                dtoUser.ProfileImageExtention = GetProfileImageVirtualPath(dtoUser.UserId);
                return Ok(dtoUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("updateUser")]
        public IActionResult UpdateUser([FromBody] DTO.User userDto)
        {
            try
            {
                //Check if who is logged in
                string? userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                //Get model user class from DB with matching email. 
                Models.User? user = context.GetUser(userEmail);
                //Clear the tracking of all objects to avoid double tracking
                context.ChangeTracker.Clear();

                //Check if the user that is logged in is the same user of the task
                //this situation is ok only if the user is a manager
                if (user == null || (userDto.UserId != user.UserId))
                {
                    return Unauthorized("Non Manager User is trying to update a different user");
                }

                Models.User appUser = userDto.GetModels();

                context.Entry(appUser).State = EntityState.Modified;

                context.SaveChanges();

                //Task was updated!
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }



        [HttpPost("UploadProfileImage")]
        public async Task<IActionResult> UploadProfileImageAsync(IFormFile file)
        {
            //Check if who is logged in
            string? userEmail = HttpContext.Session.GetString("loggedInUser");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User is not logged in");
            }
            //Get model user class from DB with matching email. 
            Models.User? user = context.GetUser(userEmail);
            //Clear the tracking of all objects to avoid double tracking
            context.ChangeTracker.Clear();

            if (user == null)
            {
                return Unauthorized("User is not found in the database");
            }
            //Read all files sent
            long imagesSize = 0;

            if (file.Length > 0)
            {
                //Check the file extention!
                string[] allowedExtentions = { ".png", ".jpg" };
                string extention = "";
                if (file.FileName.LastIndexOf(".") > 0)
                {
                    extention = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
                    user.ProfileImageExtention = extention;
                }
                if (!allowedExtentions.Where(e => e == extention).Any())
                {
                    //Extention is not supported
                    return BadRequest("File sent with non supported extention");
                }

                //Build path in the web root (better to a specific folder under the web root
                string filePath = $"{this.webHostEnvironment.WebRootPath}\\profileImages\\{user.UserId}{extention}";

                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);

                    if (IsImage(stream))
                    {
                        imagesSize += stream.Length;
                    }
                    else
                    {
                        //Delete the file if it is not supported!
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            //Update image extention in DB
            context.Entry(user).State = EntityState.Modified;
            context.SaveChanges();
            DTO.User dtoUser = new DTO.User(user);
            dtoUser.ProfileImageExtention = GetProfileImageVirtualPath(dtoUser.UserId);
            return Ok(dtoUser);
        }

        [HttpPost("AddGame")]
        public IActionResult AddGame([FromBody] DTO.GameDetails gameDetailsDTO)
        {
            try
            {
                //HttpContext.Session.Clear(); //Logout any previous login attempt

                //Create model user class
                Models.GameDetail modelsgameDetails = gameDetailsDTO.GetModels();

                context.GameDetails.Add(modelsgameDetails);
                context.SaveChanges();

                //Game was added!
                DTO.GameDetails dtogameDetails = new DTO.GameDetails(modelsgameDetails);
                return Ok(dtogameDetails);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("games/{gameId}/players")]
        public IActionResult GetPlayersByGameId(int gameId)
        {
            try
            {
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
                if (game == null)
                    return NotFound($"Game with ID {gameId} not found.");

                var rawPlayers = context.GameUsers
                    .Where(gu => gu.GameId == gameId)
                    .Join(context.Users,
                          gu => gu.UserId,
                          u => u.UserId,
                          (gu, u) => new { User = u, gu.RoleId })
                    .Join(context.GameRoles,
                          gu => gu.RoleId,
                          r => r.RoleId,
                          (gu, r) => new { gu.User, Role = r }) // ⬅️ שומר את כל האובייקט של Role
                    .ToList();

                var players = rawPlayers.Select(p =>
                {
                    var dto = new DTO.User(p.User);
                    dto.RoleName = p.Role.Name;
                    dto.ProfileImageExtention = GetProfileImageVirtualPath(dto.UserId);

                    // ⬇️ הוספה של מיקום מהתפקיד
                    dto.PositionX = p.Role.PositionX;
                    dto.PositionY = p.Role.PositionY;
                    

                    return dto;
                }).ToList();

                return Ok(players);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }





        [HttpGet("games")]
        public IActionResult GetGames()
        {
            try
            {
                var games = context.GameDetails.Include(g => g.Creator).ToList();
                var dtoGames = games.Select(g => new DTO.GameDetails(g)).ToList();
                return Ok(dtoGames);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetGameDetailsByGameId")]
        public IActionResult GetGameDetailsByGameId([FromQuery] int gameId)
        {
            try
            {
                GameDetail? g = context.GameDetails.Include(gg => gg.Creator).Where(gg => gg.GameId == gameId).FirstOrDefault();
                if (g == null)
                {
                    return BadRequest($"No Such Game ID: {gameId}");
                }
                DTO.GameDetails dtoGameDetails = new DTO.GameDetails(g); 
                return Ok(dtoGameDetails);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("joinGame")]
        public IActionResult JoinGame(int gameId)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                var user = context.Users.SingleOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var game = context.GameDetails.SingleOrDefault(g => g.GameId == gameId);
                if (game == null)
                {
                    return NotFound("Game not found");
                }

                // Check if the user is already registered for the game
                bool alreadyJoined = context.GameUsers.Any(gu => gu.GameId == gameId && gu.UserId == user.UserId);
                if (alreadyJoined)
                {
                    return BadRequest("User is already registered for this game.");
                }

                // Find an available role based on the game type (GameType)
                var availableRole = context.GameRoles
                    .Where(r => r.GameTypeId == game.GameType) // Use GameType instead of GameTypeId
                    .OrderBy(r => Guid.NewGuid()) // Select a random role
                    .FirstOrDefault();

                if (availableRole == null)
                {
                    return BadRequest("No available roles for this game type.");
                }

                var gameUser = new GameUser
                {
                    GameId = game.GameId,
                    UserId = user.UserId,
                    RoleId = availableRole.RoleId // Assign the selected role
                };

                context.GameUsers.Add(gameUser);
                context.SaveChanges();

                return Ok(new { Message = "Successfully joined the game!", RoleAssigned = availableRole.Name });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // New endpoint to get game capacity information
        [HttpGet("games/{gameId}/capacity")]
        public IActionResult GetGameCapacity(int gameId)
        {
            try
            {
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
                if (game == null)
                    return NotFound($"Game with ID {gameId} not found.");

                var currentPlayerCount = context.GameUsers.Count(gu => gu.GameId == gameId);

                // Get maximum player count based on game type
                int maxPlayers;
                switch (game.GameType)
                {
                    case 1: // Basketball
                        maxPlayers = 5;
                        break;
                    case 2: // Soccer
                        maxPlayers = 11;
                        break;
                    case 3: // Volleyball
                        maxPlayers = 6;
                        break;
                    default:
                        maxPlayers = 10; // Default maximum
                        break;
                }

                // Get available roles
                var availableRoles = GetAvailableRoles(gameId, game.GameType.Value);

                return Ok(new
                {
                    CurrentPlayers = currentPlayerCount,
                    MaxPlayers = maxPlayers,
                    AvailablePositions = availableRoles.Count,
                    IsFull = currentPlayerCount >= maxPlayers || availableRoles.Count == 0
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpGet("GetUserByEmail")]
        public IActionResult GetUserByEmail([FromQuery] string email)
        {
            try
            {

                //Read user by email

                Models.User u = context.GetUser(email);
                DTO.User user = new DTO.User(u);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("GetAllEmails")]
        public IActionResult GetAllEmails()
        {
            try
            {
                //Read all emails of every user in the app

                List<string> list = context.GetAllEmails();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        
        [HttpPost("UpdateUserPassword")]
        public IActionResult UpdateUserPassword([FromBody] DTO.User userDto)
        {
            try
            {
                //Get model user class from DB with matching email. 
                Models.User? theUser = context.GetUser(userDto.Email);
                //Clear the tracking of all objects to avoid double tracking
                context.ChangeTracker.Clear();

                //Check if the user that is logged in is the same user of the task

                if (theUser == null || (userDto.UserId != theUser.UserId))
                {
                    return Unauthorized("Failed to update user");
                }

                Models.User appUser = userDto.GetModels();

                context.Entry(appUser).State = EntityState.Modified;

                context.SaveChanges();

                //Task was updated!
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("gameTypes")]
        public IActionResult GetGameTypes()
        {
            try
            {
                var gameTypes = context.GameTypes.ToList();
                if (gameTypes == null || gameTypes.Count == 0)
                {
                    return NotFound("No game types found.");
                }

                return Ok(gameTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("myGames")]
        public IActionResult GetMyGames()
        {
            try
            {
                // בדיקה אם המשתמש מחובר
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                // הבאת המשתמש מה־DB לפי האימייל
                var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // הבאת כל המשחקים שהוא הצטרף אליהם (מטבלת GameUsers)
                var joinedGames = context.GameUsers
                    .Include(gu => gu.Game) // כוללים את פרטי המשחק
                        .ThenInclude(g => g.Creator) // כוללים גם את היוצר של המשחק
                    .Where(gu => gu.UserId == user.UserId)
                    .Select(gu => new DTO.GameDetails(gu.Game))
                    .ToList();

                return Ok(joinedGames);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpDelete("games/{id}")]
        public IActionResult DeleteGame(int id)
        {
            try
            {
                var game = context.GameDetails
                    .Include(g => g.GameUsers) // אם יש קשרים כאלה
                    .FirstOrDefault(g => g.GameId == id);

                if (game == null)
                {
                    return NotFound($"No game found with ID: {id}");
                }

                // אם יש GameUsers – תמחק גם אותם
                var gameUsers = context.GameUsers.Where(gu => gu.GameId == id).ToList();
                context.GameUsers.RemoveRange(gameUsers);

                context.GameDetails.Remove(game);
                context.SaveChanges();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("LeaveGame")]
        public IActionResult LeaveGame(int gameId)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                var user = context.Users.SingleOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var gameUser = context.GameUsers
                    .SingleOrDefault(gu => gu.GameId == gameId && gu.UserId == user.UserId);

                if (gameUser == null)
                {
                    return NotFound("User is not registered to this game.");
                }

                context.GameUsers.Remove(gameUser);
                context.SaveChanges();

                return Ok(new { Message = "Successfully left the game." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("counts")]
        public IActionResult GetCounts()
        {
            try
            {
                var playerCount = context.Users.Count();
                var gameCount = context.GameDetails.Count();

                var counts = new
                {
                    PlayerCount = playerCount,
                    GameCount = gameCount
                };

                return Ok(counts);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("games/{gameId}/photos")]
        public async Task<IActionResult> UploadGamePhotoAsync(int gameId, IFormFile file, [FromForm] string description)
        {
            // Check if who is logged in
            string? userEmail = HttpContext.Session.GetString("loggedInUser");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User is not logged in");
            }

            // Get model user class from DB with matching email
            Models.User? user = context.GetUser(userEmail);
            if (user == null)
            {
                return Unauthorized("User is not found in the database");
            }

            // Check if the game exists
            var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
            if (game == null)
            {
                return NotFound($"Game with ID {gameId} not found.");
            }

            // Check if user is a participant of the game
            bool isParticipant = context.GameUsers.Any(gu => gu.GameId == gameId && gu.UserId == user.UserId);
            if (!isParticipant && game.CreatorId != user.UserId)
            {
                return Unauthorized("Only game participants or the creator can upload photos.");
            }

            // Process the photo upload
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            // Check the file extension
            string[] allowedExtensions = { ".png", ".jpg", ".jpeg" };
            string extension = "";
            if (file.FileName.LastIndexOf(".") > 0)
            {
                extension = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
            }
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("File sent with non-supported extension. Only .png, .jpg, and .jpeg are supported.");
            }

            // Create a new photo record
            var photo = new Models.Photo
            {
                Description = description,
                GameId = gameId
            };

            context.Photos.Add(photo);
            context.SaveChanges();

            // Save the file in a dedicated folder for game photos
            // Make sure the directory exists
            string gamePhotosDir = $"{this.webHostEnvironment.WebRootPath}\\gamePhotos\\{gameId}";
            Directory.CreateDirectory(gamePhotosDir);

            // Save the file
            string fileName = $"{photo.PhotoId}{extension}";
            string filePath = $"{gamePhotosDir}\\{fileName}";

            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);

                if (IsImage(stream))
                {
                    // Update the image URL in the database
                    photo.ImageUrl = $"/gamePhotos/{gameId}/{fileName}";
                    context.SaveChanges();

                    DTO.Photo dtoPhoto = new DTO.Photo(photo);
                    return Ok(dtoPhoto);
                }
                else
                {
                    // Delete the file and DB record if not a valid image
                    System.IO.File.Delete(filePath);
                    context.Photos.Remove(photo);
                    context.SaveChanges();
                    return BadRequest("The uploaded file is not a valid image.");
                }
            }
        }

        [HttpGet("games/{gameId}/photos")]
        public IActionResult GetGamePhotos(int gameId)
        {
            try
            {
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
                if (game == null)
                    return NotFound($"Game with ID {gameId} not found.");

                var photos = context.Photos
                    .Where(p => p.GameId == gameId)
                    .ToList();

                var dtoPhotos = photos.Select(p => new DTO.Photo(p)).ToList();
                return Ok(dtoPhotos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("photos/{photoId}")]
        public IActionResult DeletePhoto(int photoId)
        {
            try
            {
                // Check who is logged in
                string? userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                // Get the user from the database
                Models.User? user = context.GetUser(userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // Find the photo
                var photo = context.Photos.Include(p => p.Game).FirstOrDefault(p => p.PhotoId == photoId);
                if (photo == null)
                {
                    return NotFound($"Photo not found");
                }

                // Check if user is the game creator
                bool isGameCreator = photo.Game?.CreatorId == user.UserId;

                // Only game creator can delete photos
                if (!isGameCreator)
                {
                    return Unauthorized("Only game creator can delete photos");
                }

                // Delete the physical file if it exists
                if (!string.IsNullOrEmpty(photo.ImageUrl))
                {
                    string filePath = $"{this.webHostEnvironment.WebRootPath}{photo.ImageUrl.Replace('/', '\\')}";
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Remove the photo record from the database
                context.Photos.Remove(photo);
                context.SaveChanges();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        //Helper functions

        //this function gets a file stream and check if it is an image
        private static bool IsImage(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            List<string> jpg = new List<string> { "FF", "D8" };
            List<string> bmp = new List<string> { "42", "4D" };
            List<string> gif = new List<string> { "47", "49", "46" };
            List<string> png = new List<string> { "89", "50", "4E", "47", "0D", "0A", "1A", "0A" };
            List<List<string>> imgTypes = new List<List<string>> { jpg, bmp, gif, png };

            List<string> bytesIterated = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                string bit = stream.ReadByte().ToString("X2");
                bytesIterated.Add(bit);

                bool isImage = imgTypes.Any(img => !img.Except(bytesIterated).Any());
                if (isImage)
                {
                    return true;
                }
            }
            return false;
        }
        //this function check which profile image exist and return the virtual path of it.
        //if it does not exist it returns the default profile image virtual path
        private string GetProfileImageVirtualPath(int userId)
        {
            string virtualPath = $"/profileImages/{userId}";
            string path = $"{this.webHostEnvironment.WebRootPath}\\profileImages\\{userId}.png";
            if (System.IO.File.Exists(path))
            {
                virtualPath += ".png";
            }
            else
            {
                path = $"{this.webHostEnvironment.WebRootPath}\\profileImages\\{userId}.jpg";
                if (System.IO.File.Exists(path))
                {
                    virtualPath += ".jpg";
                }
                else
                {
                    virtualPath = $"/profileImages/default.png";
                }
            }

            return virtualPath;
        }
    }
}
