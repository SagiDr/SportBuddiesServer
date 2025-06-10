using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportBuddiesServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SportBuddiesServer.Controllers
{
    [Route("api")]
    [ApiController]
    public class SportBuddiesAPIController : ControllerBase
    {
        // A variable to hold a reference to the db context
        private SportBuddiesDbContext context;
        // A variable that hold a reference to web hosting interface (that provide information like the folder on which the server runs etc...)
        private IWebHostEnvironment webHostEnvironment;

        // Use dependency injection to get the db context and web host into the constructor
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
                    maxPlayers = 10; // Changed to 10 - 5 players per team
                    break;
                case 2: // Soccer
                    maxPlayers = 22; // Changed to 22 - 11 players per team
                    break;
                case 3: // Volleyball
                    maxPlayers = 12; // Changed to 12 - 6 players per team
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
                HttpContext.Session.Clear(); // Logout any previous login attempt

                // Get model user class from DB with matching email
                Models.User? modelsUser = context.GetUser(loginDto.Email);

                // Check if user exist for this email and if password match, if not return Access Denied (Error 403)
                if (modelsUser == null || modelsUser.Password != loginDto.Password)
                {
                    return Unauthorized();
                }

                // Login succeed! now mark login in session memory
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
                HttpContext.Session.Clear(); // Logout any previous login attempt

                // Create model user class
                Models.User modelsUser = userDto.GetModels();

                // Set default profile image path if not provided
                if (string.IsNullOrEmpty(modelsUser.ProfileImageExtention))
                {
                    modelsUser.ProfileImageExtention = "/profileImages/default.png";
                }

                context.Users.Add(modelsUser);
                context.SaveChanges();

                // User was added!
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
                // Check if who is logged in
                string? userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                // Get model user class from DB with matching email
                Models.User? user = context.GetUser(userEmail);
                // Clear the tracking of all objects to avoid double tracking
                context.ChangeTracker.Clear();

                // Check if the user that is logged in is the same user of the task
                // this situation is ok only if the user is a manager
                if (user == null || (userDto.UserId != user.UserId))
                {
                    return Unauthorized("Non Manager User is trying to update a different user");
                }

                Models.User appUser = userDto.GetModels();
                context.Entry(appUser).State = EntityState.Modified;
                context.SaveChanges();

                // Task was updated!
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
            // Check if who is logged in
            string? userEmail = HttpContext.Session.GetString("loggedInUser");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User is not logged in");
            }

            // Get model user class from DB with matching email
            Models.User? user = context.GetUser(userEmail);
            // Clear the tracking of all objects to avoid double tracking
            context.ChangeTracker.Clear();

            if (user == null)
            {
                return Unauthorized("User is not found in the database");
            }

            // Read all files sent
            long imagesSize = 0;

            if (file.Length > 0)
            {
                // Check the file extension!
                string[] allowedExtentions = { ".png", ".jpg" };
                string extention = "";
                if (file.FileName.LastIndexOf(".") > 0)
                {
                    extention = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
                    user.ProfileImageExtention = extention;
                }
                if (!allowedExtentions.Where(e => e == extention).Any())
                {
                    // Extension is not supported
                    return BadRequest("File sent with non supported extension");
                }

                // Build path in the web root (better to a specific folder under the web root
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
                        // Delete the file if it is not supported!
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            // Update image extension in DB
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
                // Check if user is logged in
                string? userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                // Get the logged-in user
                Models.User? user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // Basic validation
                if (gameDetailsDTO == null)
                    return BadRequest("No game data received");

                if (string.IsNullOrWhiteSpace(gameDetailsDTO.GameName))
                    return BadRequest("Game name is required");

                if (string.IsNullOrWhiteSpace(gameDetailsDTO.Location))
                    return BadRequest("Location is required");

                if (!gameDetailsDTO.GameType.HasValue || gameDetailsDTO.GameType <= 0)
                    return BadRequest("Valid game type is required");

                if (!gameDetailsDTO.Date.HasValue)
                    return BadRequest("Date is required");

                if (!gameDetailsDTO.Time.HasValue)
                    return BadRequest("Time is required");

                // Prepare values
                string gameName = gameDetailsDTO.GameName.Trim();
                string location = gameDetailsDTO.Location.Trim();
                DateOnly date = gameDetailsDTO.Date.Value;
                TimeOnly time = gameDetailsDTO.Time.Value;
                int gameType = gameDetailsDTO.GameType.Value;
                string competitive = string.IsNullOrWhiteSpace(gameDetailsDTO.Competitive) ? "Casual" : gameDetailsDTO.Competitive.Trim();
                string state = string.IsNullOrWhiteSpace(gameDetailsDTO.State) ? "Public" : gameDetailsDTO.State.Trim();
                int creatorId = user.UserId;

                // Generate invitation code for private games
                string invitationCode = null;
                if (state.Equals("Private", StringComparison.OrdinalIgnoreCase))
                {
                    invitationCode = GenerateRandomInvitationCode();
                }

                // Use raw SQL to insert - this bypasses Entity Framework triggers issue
                string insertSql = @"
            INSERT INTO GameDetails (GameName, Location, Date, Time, GameType, Competitive, State, CreatorId, Link, Score, Notes, LocationLength, LocationWidth)
            VALUES (@GameName, @Location, @Date, @Time, @GameType, @Competitive, @State, @CreatorId, @Link, @Score, @Notes, @LocationLength, @LocationWidth);
            SELECT SCOPE_IDENTITY();";

                int newGameId;

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = insertSql;

                    // Add parameters to prevent SQL injection
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@GameName", gameName));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Location", location));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Date", date));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Time", time));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@GameType", gameType));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Competitive", competitive));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@State", state));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@CreatorId", creatorId));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Link", (object)invitationCode ?? DBNull.Value));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Score", DBNull.Value));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Notes", DBNull.Value));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@LocationLength", DBNull.Value));
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@LocationWidth", DBNull.Value));

                    context.Database.OpenConnection();
                    var result = command.ExecuteScalar();
                    newGameId = Convert.ToInt32(result);
                    context.Database.CloseConnection();
                }

                if (newGameId <= 0)
                {
                    return BadRequest("Failed to create game - no ID returned");
                }

                // Create response DTO
                DTO.GameDetails responseDto = new DTO.GameDetails
                {
                    GameId = newGameId,
                    GameName = gameName,
                    Location = location,
                    Date = date,
                    Time = time,
                    GameType = gameType,
                    Competitive = competitive,
                    State = state,
                    CreatorId = creatorId,
                    Link = invitationCode,
                    Score = null,
                    Notes = null,
                    Creator = new DTO.User
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email
                    }
                };

                // Try to create chat using raw SQL as well
                try
                {
                    string chatInsertSql = @"
                INSERT INTO GameChats (GameId, ChatName, CreatedDate)
                VALUES (@GameId, @ChatName, @CreatedDate)";

                    using (var chatCommand = context.Database.GetDbConnection().CreateCommand())
                    {
                        chatCommand.CommandText = chatInsertSql;
                        chatCommand.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@GameId", newGameId));
                        chatCommand.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@ChatName", $"{gameName} Chat"));
                        chatCommand.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@CreatedDate", DateTime.Now));

                        if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                            context.Database.OpenConnection();

                        chatCommand.ExecuteNonQuery();
                        context.Database.CloseConnection();
                    }
                }
                catch (Exception chatEx)
                {
                    Console.WriteLine($"Chat creation failed: {chatEx.Message}");
                    // Don't fail the main operation
                }

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddGame error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return BadRequest($"Error creating game: {ex.Message}");
            }
        }

        // Helper method
        private string GenerateRandomInvitationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
                          (gu, r) => new { gu.User, Role = r }) // Keeps the entire Role object
                    .ToList();

                var players = rawPlayers.Select(p =>
                {
                    var dto = new DTO.User(p.User);
                    dto.RoleName = p.Role.Name;
                    dto.ProfileImageExtention = GetProfileImageVirtualPath(dto.UserId);

                    // Addition of position from role
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
                // Returns all games (public and private)
                var games = context.GameDetails
                    .Include(g => g.Creator)
                    .ToList();

                var dtoGames = games.Select(g => new DTO.GameDetails(g)).ToList();
                return Ok(dtoGames);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("joinPrivateGame")]
        public IActionResult JoinPrivateGame(int gameId, string invitationCode, string team = "A")
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

                // Check if the game is private and the invitation code matches
                if (game.State == "Private" && game.Link != invitationCode)
                {
                    return BadRequest(new { Error = "Invalid invitation code. Please ask the game creator for the correct code." });
                }

                // Check if user is already registered for this game
                bool alreadyJoined = context.GameUsers.Any(gu => gu.GameId == gameId && gu.UserId == user.UserId);
                if (alreadyJoined)
                {
                    return BadRequest(new { Error = "You are already registered for this game. You can view it in 'My Games'." });
                }

                // Validate team choice
                if (team != "A" && team != "B")
                {
                    team = "A"; // Default to team A if invalid team specified
                }

                // Check if the selected team is full
                int teamSize = context.GameUsers.Count(gu => gu.GameId == gameId && gu.Team == team);
                int maxTeamSize;

                switch (game.GameType)
                {
                    case 1: // Basketball
                        maxTeamSize = 5;
                        break;
                    case 2: // Soccer
                        maxTeamSize = 11;
                        break;
                    case 3: // Volleyball
                        maxTeamSize = 6;
                        break;
                    default:
                        maxTeamSize = 5;
                        break;
                }

                if (teamSize >= maxTeamSize)
                {
                    return BadRequest(new { Error = $"Team {team} is already full. Please try joining the other team." });
                }

                // Get available roles
                var availableRoles = GetAvailableRoles(gameId, game.GameType.Value);
                if (!availableRoles.Any())
                {
                    return BadRequest(new { Error = "All positions for this game have been filled." });
                }

                var availableRole = availableRoles.OrderBy(r => Guid.NewGuid()).FirstOrDefault();

                var gameUser = new GameUser
                {
                    GameId = game.GameId,
                    UserId = user.UserId,
                    RoleId = availableRole.RoleId,
                    Team = team
                };

                context.GameUsers.Add(gameUser);
                context.SaveChanges();

                return Ok(new { Message = $"Successfully joined the game on Team {team}!", RoleAssigned = availableRole.Name });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("privateGame")]
        public IActionResult GetPrivateGame(string invitationCode)
        {
            try
            {
                // Search for the game by invitation code
                var game = context.GameDetails
                    .Include(g => g.Creator)
                    .FirstOrDefault(g => g.State == "Private" && g.Link == invitationCode);

                if (game == null)
                {
                    return NotFound("No game found with this invitation code");
                }

                DTO.GameDetails dtoGame = new DTO.GameDetails(game);
                return Ok(dtoGame);
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
        public IActionResult JoinGame(int gameId, string team = "A")
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

                // Check if user is already registered for this game
                bool alreadyJoined = context.GameUsers.Any(gu => gu.GameId == gameId && gu.UserId == user.UserId);
                if (alreadyJoined)
                {
                    return BadRequest(new { Error = "You are already registered for this game." });
                }

                // Validate team choice
                if (team != "A" && team != "B")
                {
                    team = "A"; // Default to team A if invalid team specified
                }

                // Check if the selected team is full
                int teamSize = context.GameUsers.Count(gu => gu.GameId == gameId && gu.Team == team);
                int maxTeamSize;

                switch (game.GameType)
                {
                    case 1: // Basketball
                        maxTeamSize = 5;
                        break;
                    case 2: // Soccer
                        maxTeamSize = 11;
                        break;
                    case 3: // Volleyball
                        maxTeamSize = 6;
                        break;
                    default:
                        maxTeamSize = 5;
                        break;
                }

                if (teamSize >= maxTeamSize)
                {
                    return BadRequest(new { Error = $"Team {team} is already full. Please try joining the other team." });
                }

                // Get available roles
                var availableRoles = GetAvailableRoles(gameId, game.GameType.Value);
                if (!availableRoles.Any())
                {
                    return BadRequest(new { Error = "All positions for this game have been filled." });
                }

                var availableRole = availableRoles.OrderBy(r => Guid.NewGuid()).FirstOrDefault();

                var gameUser = new GameUser
                {
                    GameId = game.GameId,
                    UserId = user.UserId,
                    RoleId = availableRole.RoleId,
                    Team = team
                };

                context.GameUsers.Add(gameUser);
                context.SaveChanges();

                return Ok(new { Message = $"Successfully joined the game on Team {team}!", RoleAssigned = availableRole.Name });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("UpdateGameScoreAndNotes")]
        public IActionResult UpdateGameScoreAndNotes([FromBody] DTO.GameDetails gameDetailsDTO)
        {
            try
            {
                // Check if who is logged in
                string? userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                // Get model user class from DB with matching email
                Models.User? user = context.GetUser(userEmail);

                // Clear the tracking of all objects to avoid double tracking
                context.ChangeTracker.Clear();

                // Check if the user is the creator of the game
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameDetailsDTO.GameId);
                if (game == null)
                {
                    return NotFound($"Game with ID {gameDetailsDTO.GameId} not found");
                }

                // Verify that the current user is the creator of the game
                if (user == null || game.CreatorId != user.UserId)
                {
                    return Unauthorized("Only the creator of the game can update scores and notes");
                }

                // Update score and notes
                game.Score = gameDetailsDTO.Score;
                game.Notes = gameDetailsDTO.Notes;

                // Save changes to the database
                context.Entry(game).State = EntityState.Modified;
                context.SaveChanges();

                return Ok(new DTO.GameDetails(game));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
                // Read user by email
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
                // Read all emails of every user in the app
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
                // Get model user class from DB with matching email
                Models.User? theUser = context.GetUser(userDto.Email);
                // Clear the tracking of all objects to avoid double tracking
                context.ChangeTracker.Clear();

                // Check if the user that is logged in is the same user of the task
                if (theUser == null || (userDto.UserId != theUser.UserId))
                {
                    return Unauthorized("Failed to update user");
                }

                Models.User appUser = userDto.GetModels();
                context.Entry(appUser).State = EntityState.Modified;
                context.SaveChanges();

                // Task was updated!
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
                // Check if the user is logged in
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                // Get the user from the DB by email
                var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Organize the result in two separate collections
                var result = new
                {
                    // Games that the user created
                    CreatedGames = context.GameDetails
                        .Include(g => g.Creator)
                        .Where(g => g.CreatorId == user.UserId)
                        .Select(g => new DTO.GameDetails(g))
                        .ToList(),

                    // Games that the user joined (but didn't create)
                    JoinedGames = context.GameUsers
                        .Include(gu => gu.Game)
                            .ThenInclude(g => g.Creator)
                        .Where(gu => gu.UserId == user.UserId && gu.Game.CreatorId != user.UserId)
                        .Select(gu => new DTO.GameDetails(gu.Game))
                        .ToList()
                };

                return Ok(result);
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
                // Find the game first
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == id);
                if (game == null)
                {
                    return NotFound($"No game found with ID: {id}");
                }

                // Delete related records in the correct order (similar to how other methods work)

                // 1. Delete chat messages for this game's chats
                var gameChats = context.GameChats.Where(gc => gc.GameId == id).ToList();
                foreach (var chat in gameChats)
                {
                    var chatMessages = context.ChatMessages.Where(cm => cm.ChatId == chat.ChatId).ToList();
                    context.ChatMessages.RemoveRange(chatMessages);
                }

                // 2. Delete game chats
                context.GameChats.RemoveRange(gameChats);

                // 3. Delete photos (without trying to delete physical files)
                var photos = context.Photos.Where(p => p.GameId == id).ToList();
                context.Photos.RemoveRange(photos);

                // 4. Delete game users (like in LeaveGame method)
                var gameUsers = context.GameUsers.Where(gu => gu.GameId == id).ToList();
                context.GameUsers.RemoveRange(gameUsers);

                // 5. Finally delete the game itself
                context.GameDetails.Remove(game);

                // Save all changes
                context.SaveChanges();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting game {id}: {ex.Message}");
                return BadRequest($"Error deleting game: {ex.Message}");
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

                // Get the game details to check if the user is the creator
                var game = context.GameDetails.SingleOrDefault(g => g.GameId == gameId);
                if (game == null)
                {
                    return NotFound("Game not found");
                }

                // Check if the user is the creator of the game
                if (game.CreatorId == user.UserId)
                {
                    return BadRequest(new { Error = "Game creators cannot leave their own games." });
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

        [HttpGet("games/{gameId}/teams")]
        public IActionResult GetTeamCounts(int gameId)
        {
            try
            {
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
                if (game == null)
                    return NotFound($"Game with ID {gameId} not found.");

                // Count players in each team
                var teamCounts = context.GameUsers
                    .Where(gu => gu.GameId == gameId)
                    .GroupBy(gu => gu.Team)
                    .Select(g => new { Team = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.Team, x => x.Count);

                // Ensure both teams are in the dictionary
                if (!teamCounts.ContainsKey("A"))
                    teamCounts["A"] = 0;
                if (!teamCounts.ContainsKey("B"))
                    teamCounts["B"] = 0;

                // Get maximum players per team based on game type
                int maxPlayersPerTeam;
                switch (game.GameType)
                {
                    case 1: // Basketball
                        maxPlayersPerTeam = 5;
                        break;
                    case 2: // Soccer
                        maxPlayersPerTeam = 11;
                        break;
                    case 3: // Volleyball
                        maxPlayersPerTeam = 6;
                        break;
                    default:
                        maxPlayersPerTeam = 5;
                        break;
                }

                // Add max players information
                teamCounts["MaxPerTeam"] = maxPlayersPerTeam;

                return Ok(teamCounts);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("games/{gameId}/banPlayer")]
        public IActionResult BanPlayer(int gameId, int playerId)
        {
            try
            {
                Console.WriteLine($"DEBUG SERVER: BanPlayer called with gameId: {gameId}, playerId: {playerId}");

                // Check if who is logged in
                string? userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    Console.WriteLine("DEBUG SERVER: User is not logged in");
                    return Unauthorized("User is not logged in");
                }

                // Get model user class from DB with matching email
                Models.User? user = context.GetUser(userEmail);
                if (user == null)
                {
                    Console.WriteLine("DEBUG SERVER: User not found in database");
                    return Unauthorized("User not found");
                }

                Console.WriteLine($"DEBUG SERVER: Current user: {user.Name} (ID: {user.UserId}), IsAdmin: {user.IsAdmin}");

                // Get the game details
                var game = context.GameDetails.FirstOrDefault(g => g.GameId == gameId);
                if (game == null)
                {
                    Console.WriteLine($"DEBUG SERVER: Game {gameId} not found");
                    return NotFound("Game not found");
                }

                Console.WriteLine($"DEBUG SERVER: Game found: {game.GameName}, Creator ID: {game.CreatorId}");

                // Check if the user is the creator of the game or an admin
                bool isCreator = game.CreatorId == user.UserId;
                bool isAdmin = user.IsAdmin == "YES";

                Console.WriteLine($"DEBUG SERVER: IsCreator: {isCreator}, IsAdmin: {isAdmin}");

                if (!isCreator && !isAdmin)
                {
                    Console.WriteLine("DEBUG SERVER: User has no permission to ban players");
                    return Unauthorized("Only game creators or admins can ban players from a game");
                }

                // Check if the player to ban exists and is actually in the game
                var playerToRemove = context.GameUsers
                    .FirstOrDefault(gu => gu.GameId == gameId && gu.UserId == playerId);

                if (playerToRemove == null)
                {
                    Console.WriteLine($"DEBUG SERVER: Player {playerId} not found in game {gameId}");
                    return NotFound("Player not found in this game");
                }

                Console.WriteLine($"DEBUG SERVER: Player to remove found: UserID {playerToRemove.UserId}, RoleID {playerToRemove.RoleId}");

                // Creator can't ban themselves
                if (playerId == game.CreatorId)
                {
                    Console.WriteLine("DEBUG SERVER: Attempting to ban game creator");
                    return BadRequest(new { Error = "Game creator cannot be banned from their own game" });
                }

                // Remove the player from the game
                context.GameUsers.Remove(playerToRemove);
                int saveResult = context.SaveChanges();

                Console.WriteLine($"DEBUG SERVER: Save changes result: {saveResult} rows affected");

                return Ok(new { Message = "Player has been removed from the game" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG SERVER: Exception: {ex.Message}");
                Console.WriteLine($"DEBUG SERVER: Stack trace: {ex.StackTrace}");
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Get all chats for the current user (only for games they participate in)
        [HttpGet("mychats")]
        public IActionResult GetMyChats()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // Get games where user participates (either created or joined)
                var userGameIds = context.GameUsers
                    .Where(gu => gu.UserId == user.UserId)
                    .Select(gu => gu.GameId)
                    .Union(context.GameDetails
                        .Where(gd => gd.CreatorId == user.UserId)
                        .Select(gd => gd.GameId))
                    .ToList();

                // Get chats for these games
                var chats = context.GameChats
                    .Include(gc => gc.Game)
                    .Where(gc => userGameIds.Contains(gc.GameId))
                    .ToList();

                var dtoChats = new List<DTO.GameChat>();

                foreach (var chat in chats)
                {
                    var dtoChat = new DTO.GameChat(chat);

                    // Get last message
                    var lastMessage = context.ChatMessages
                        .Where(cm => cm.ChatId == chat.ChatId)
                        .OrderByDescending(cm => cm.SentAt)
                        .FirstOrDefault();

                    if (lastMessage != null)
                    {
                        dtoChat.LastMessage = lastMessage.MessageContent;
                        dtoChat.LastMessageTime = lastMessage.SentAt;
                    }

                    // Get unread count for this user
                    dtoChat.UnreadCount = context.ChatMessages
                        .Where(cm => cm.ChatId == chat.ChatId &&
                                    cm.SenderId != user.UserId &&
                                    cm.IsRead == false)
                        .Count();

                    dtoChats.Add(dtoChat);
                }

                return Ok(dtoChats.OrderByDescending(c => c.LastMessageTime));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Get messages for a specific chat
        [HttpGet("chat/{chatId}/messages")]
        public IActionResult GetChatMessages(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // Check if user has access to this chat
                var chat = context.GameChats
                    .Include(gc => gc.Game)
                    .FirstOrDefault(gc => gc.ChatId == chatId);

                if (chat == null)
                {
                    return NotFound("Chat not found");
                }

                // Verify user is participant in the game
                bool hasAccess = context.GameUsers.Any(gu => gu.GameId == chat.GameId && gu.UserId == user.UserId) ||
                                chat.Game.CreatorId == user.UserId;

                if (!hasAccess)
                {
                    return Unauthorized("User does not have access to this chat");
                }

                // Get messages with pagination
                var messages = context.ChatMessages
                 .Include(cm => cm.Sender)
                 .Where(cm => cm.ChatId == chatId)
                 .OrderByDescending(cm => cm.SentAt)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .AsEnumerable()   // LINQ → IEnumerable
                 .Reverse()        // LINQ.Reverse מחזיר IEnumerable
                 .ToList();

                var dtoMessages = messages.Select(m => new DTO.ChatMessage(m)).ToList();

                // Mark messages as read for current user
                var unreadMessages = context.ChatMessages
                    .Where(cm => cm.ChatId == chatId &&
                                cm.SenderId != user.UserId &&
                                cm.IsRead == false)
                    .ToList();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                context.SaveChanges();

                return Ok(dtoMessages);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Send a message to a chat
        [HttpPost("chat/{chatId}/send")]
        public IActionResult SendMessage(int chatId, [FromBody] DTO.ChatMessage messageDto)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // Check if user has access to this chat
                var chat = context.GameChats
                    .Include(gc => gc.Game)
                    .FirstOrDefault(gc => gc.ChatId == chatId);

                if (chat == null)
                {
                    return NotFound("Chat not found");
                }

                // Verify user is participant in the game
                bool hasAccess = context.GameUsers.Any(gu => gu.GameId == chat.GameId && gu.UserId == user.UserId) ||
                                chat.Game.CreatorId == user.UserId;

                if (!hasAccess)
                {
                    return Unauthorized("User does not have access to this chat");
                }

                if (string.IsNullOrWhiteSpace(messageDto.MessageContent))
                {
                    return BadRequest("Message content cannot be empty");
                }

                var message = new Models.ChatMessage
                {
                    ChatId = chatId,
                    SenderId = user.UserId,
                    MessageContent = messageDto.MessageContent.Trim(),
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                context.ChatMessages.Add(message);
                context.SaveChanges();

                // Return the created message with sender info
                var createdMessage = context.ChatMessages
                    .Include(cm => cm.Sender)
                    .FirstOrDefault(cm => cm.MessageId == message.MessageId);

                return Ok(new DTO.ChatMessage(createdMessage));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Get chat info by chat ID
        [HttpGet("chat/{chatId}")]
        public IActionResult GetChatInfo(int chatId)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("loggedInUser");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }

                var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var chat = context.GameChats
                    .Include(gc => gc.Game)
                    .FirstOrDefault(gc => gc.ChatId == chatId);

                if (chat == null)
                {
                    return NotFound("Chat not found");
                }

                // Verify user is participant in the game
                bool hasAccess = context.GameUsers.Any(gu => gu.GameId == chat.GameId && gu.UserId == user.UserId) ||
                                chat.Game.CreatorId == user.UserId;

                if (!hasAccess)
                {
                    return Unauthorized("User does not have access to this chat");
                }

                var dtoChat = new DTO.GameChat(chat);
                return Ok(dtoChat);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Helper functions

        // This function gets a file stream and check if it is an image
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

        // This function check which profile image exist and return the virtual path of it.
        // If it does not exist it returns the default profile image virtual path
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