Use master
Go
IF EXISTS (SELECT * FROM sys.databases WHERE name = N'SportBuddiesDB')
BEGIN
    DROP DATABASE SportBuddiesDB;
END
Go
Create Database SportBuddiesDB
Go
Use SportBuddiesDB
Go

-- Create GameType table
CREATE TABLE [GameType] (
    IdType INT PRIMARY KEY IDENTITY(1,1),
    [Name] NVARCHAR(255),
    IconExtention NVARCHAR(255),
    CourtExtention NVARCHAR(255)
);

-- Create User table
CREATE TABLE [User] (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    [Name] VARCHAR(255),
    Email VARCHAR(255),
    [Password] VARCHAR(255),
    Gender VARCHAR(255),
    IsAdmin VARCHAR(3) CHECK (IsAdmin IN ('YES', 'NO')),
    ProfileImageExtention VARCHAR(255),
    FavoriteSport INT,   -- Changed to INT to match GameType(IdType)
    FOREIGN KEY (FavoriteSport) REFERENCES GameType(IdType)  -- Foreign key to GameType(IdType)
);

-- Create GameDetails table
CREATE TABLE [GameDetails] (
    GameID INT PRIMARY KEY IDENTITY(1,1),
    GameName VARCHAR(255),
    [Date] DATE,
    [Time] TIME,
    [Location] VARCHAR(255),
    GameType INT,
    [State] VARCHAR(10) CHECK (State IN ('Private', 'Public')),
    Score VARCHAR(50),
    Notes TEXT,
    Competitive VARCHAR(255),
    Link VARCHAR(255),
    LocationLength DECIMAL,
    LocationWidth DECIMAL,
    CreatorId INT,
    FOREIGN KEY (CreatorId) REFERENCES [User](UserID),
    FOREIGN KEY (GameType) REFERENCES GameType(IdType)  -- Foreign key to GameType(IdType)
);

-- Create Photo table
CREATE TABLE [Photo] (
    PhotoID INT PRIMARY KEY IDENTITY(1,1),
    ImageURL VARCHAR(255),
    [Description] TEXT,
    GameID INT,
    FOREIGN KEY (GameID) REFERENCES [GameDetails](GameID)
);

-- Create Messages table
CREATE TABLE [Messages] (
    MessageID INT PRIMARY KEY IDENTITY(1,1),
    SenderID INT,
    ReceiverID INT,
    MessageContent TEXT,
    [Timestamp] DATETIME,
    FOREIGN KEY (SenderID) REFERENCES [User](UserID),
    FOREIGN KEY (ReceiverID) REFERENCES [User](UserID)
);

-- Create GameRoles table
CREATE TABLE [GameRoles] (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    GameTypeID INT,
    [Name] NVARCHAR(255),
    PositionX INT,
    PositionY INT,
    FOREIGN KEY (GameTypeID) REFERENCES GameType(IdType)
);

-- Create GameUsers table WITH THE TEAM COLUMN ALREADY INCLUDED
CREATE TABLE [GameUsers] (
    GameId INT,
    RoleId INT,
    UserId INT,
    Team CHAR(1) NOT NULL DEFAULT 'A',  -- הוספת עמודת Team מלכתחילה
    PRIMARY KEY (GameId, RoleId, UserId),
    FOREIGN KEY (GameId) REFERENCES [GameDetails](GameID),
    FOREIGN KEY (RoleId) REFERENCES GameRoles(RoleID),
    FOREIGN KEY (UserId) REFERENCES [User](UserID)
);

-- Insert into GameType table with court images
INSERT INTO [GameType] ([Name], IconExtention, CourtExtention)
VALUES 
    ('Basketball', NULL, 'basketballcourt.png'),
    ('Soccer', NULL, 'soccerfield.png'),
    ('Volleyball', NULL, 'volleyballcourt.png');

-- Inserting a user into the User table
INSERT INTO [User] (Name, Email, Password, Gender, IsAdmin, ProfileImageExtention, FavoriteSport)
VALUES ('test', 'test@test.com', '12', 'Male', 'YES', NULL, 1);
GO

INSERT INTO [User] (Name, Email, Password, Gender, IsAdmin, ProfileImageExtention, FavoriteSport)
VALUES ('sagi', 'sagi@sagi.com', '12', 'Male', 'NO', NULL, 2);
GO

INSERT INTO [User] (Name, Email, Password, Gender, IsAdmin, ProfileImageExtention, FavoriteSport)
VALUES ('ofer', 'ofer@ofer.com', '12', 'Male', 'NO', NULL, 3);
GO

-- הוספת 3 משחקי דמה
INSERT INTO [GameDetails] 
    (GameName, [Date], [Time], [Location], GameType, [State], Score, Notes, Competitive, Link, LocationLength, LocationWidth, CreatorId) 
VALUES 
    ('Basketball Match 1', '2025-02-20', '19:00', 'Court 1', 1, 'Public', '0-0', 'First game of the season', 'Competitive', 'NULL', 20.0, 10.0, 1),
    ('Soccer Match 2', '2025-02-22', '16:00', 'Stadium 5', 2, 'Private', '3-1', 'Friendly match between friends', 'Casual', '259BL8', 100.0, 60.0, 2),
    ('Volleyball Game 3', '2025-02-25', '18:00', 'Beach Court', 3, 'Public', '0-0', 'Beach volleyball tournament', 'Competitive', 'NULL', 30.0, 15.0, 1);
GO

-- Insert roles for Basketball
INSERT INTO [GameRoles] (GameTypeID, [Name], PositionX, PositionY)
VALUES 
    (1, 'Point Guard', 1, 1),
    (1, 'Shooting Guard', 2, 1),
    (1, 'Small Forward', 3, 1),
    (1, 'Power Forward', 4, 1),
    (1, 'Center', 5, 1);

-- Insert roles for Soccer
INSERT INTO [GameRoles] (GameTypeID, [Name], PositionX, PositionY)
VALUES 
    (2, 'Goalkeeper', 1, 1),
    (2, 'Right Back', 2, 1),
    (2, 'Center Back', 3, 1),
    (2, 'Center Back', 4, 1),
    (2, 'Left Back', 5, 1),
    (2, 'Right Midfield', 6, 1),
    (2, 'Center Midfield', 7, 1),
    (2, 'Center Midfield', 8, 1),
    (2, 'Left Midfield', 9, 1),
    (2, 'Right Wing', 10, 1),
    (2, 'Striker', 11, 1),
    (2, 'Left Wing', 12, 1);

-- Insert roles for Volleyball
INSERT INTO [GameRoles] (GameTypeID, [Name], PositionX, PositionY)
VALUES 
    (3, 'Setter', 1, 1),
    (3, 'Outside Hitter', 2, 1),
    (3, 'Opposite Hitter', 3, 1),
    (3, 'Middle Blocker', 4, 1),
    (3, 'Libero', 5, 1);
GO



-- Check if the login already exists before creating it
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'SportBuddiesAdminLogin')
BEGIN
    CREATE LOGIN [SportBuddiesAdminLogin] WITH PASSWORD = 'thePassword';
END
GO

-- Check if the user already exists before creating it
USE SportBuddiesDB;
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'SportBuddiesAdminUser')
BEGIN
    CREATE USER [SportBuddiesAdminUser] FOR LOGIN [SportBuddiesAdminLogin];
END
GO

-- Add the user to the db_owner role to grant admin privileges
ALTER ROLE db_owner ADD MEMBER [SportBuddiesAdminUser];
GO

-- Create a stored procedure to get team counts for a game
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetTeamCounts')
BEGIN
    DROP PROCEDURE GetTeamCounts;
END
GO

CREATE PROCEDURE GetTeamCounts
    @GameId INT
AS
BEGIN
    SELECT 
        Team,
        COUNT(*) as PlayerCount
    FROM GameUsers
    WHERE GameId = @GameId
    GROUP BY Team;
END
GO

CREATE TABLE [GameChat] (
    ChatID INT PRIMARY KEY IDENTITY(1,1),
    GameID INT NOT NULL,
    ChatName NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (GameID) REFERENCES [GameDetails](GameID)
);

-- Create ChatMessages table for individual messages
CREATE TABLE [ChatMessages] (
    MessageID INT PRIMARY KEY IDENTITY(1,1),
    ChatID INT NOT NULL,
    SenderID INT NOT NULL,
    MessageContent NTEXT NOT NULL,
    SentAt DATETIME DEFAULT GETDATE(),
    IsRead BIT DEFAULT 0,
    FOREIGN KEY (ChatID) REFERENCES [GameChat](ChatID),
    FOREIGN KEY (SenderID) REFERENCES [User](UserID)
);

-- Create chat for existing games
INSERT INTO [GameChat] (GameID, ChatName)
SELECT GameID, CONCAT(GameName, ' Chat')
FROM [GameDetails]
WHERE GameID NOT IN (SELECT GameID FROM [GameChat]);

-- Add index for better performance
CREATE INDEX IX_ChatMessages_ChatID_SentAt ON [ChatMessages](ChatID, SentAt);
CREATE INDEX IX_ChatMessages_SenderID ON [ChatMessages](SenderID);
GO

-- Create trigger to automatically create a chat when a game is created
CREATE TRIGGER tr_CreateGameChat
ON [GameDetails]
AFTER INSERT
AS
BEGIN
    INSERT INTO [GameChat] (GameID, ChatName)
    SELECT GameID, CONCAT(GameName, ' Chat')
    FROM inserted;
END
GO


SELECT * FROM [ChatMessages];
SELECT * FROM [GameDetails];
SELECT * FROM [User];
SELECT * FROM [GameType];
SELECT * FROM [GameUsers];
select * from [GameRoles];

-- scaffold-DbContext "Server = (localdb)\MSSQLLocalDB;Initial Catalog=SportBuddiesDB;User ID=SportBuddiesAdminLogin;Password=thePassword;" Microsoft.EntityFrameworkCore.SqlServer -OutPutDir Models -Context SportBuddiesDbContext -DataAnnotations –force