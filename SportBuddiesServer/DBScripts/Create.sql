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
    GameType INT,  -- Changed from VARCHAR(255) to INT to match GameType(IdType)
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

-- Create GameUsers table
CREATE TABLE [GameUsers] (
    GameId INT,
    RoleId INT,
    UserId INT,
    PRIMARY KEY (GameId, RoleId, UserId),
    FOREIGN KEY (GameId) REFERENCES [GameDetails](GameID),
    FOREIGN KEY (RoleId) REFERENCES GameRoles(RoleID),
    FOREIGN KEY (UserId) REFERENCES [User](UserID)
);


-- Insert into GameType table
INSERT INTO [GameType] ([Name], IconExtention, CourtExtention)
VALUES ('Basketball', NULL, NULL),('Soccer', NULL, NULL),('Volleyball', NULL, NULL);

-- Inserting a user into the User table
INSERT INTO [User] (Name, Email, Password, Gender, IsAdmin, ProfileImageExtention, FavoriteSport)
VALUES ('test', 'test@test.com', '12', 'Male', 'YES', NULL, 1);
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

SELECT * FROM [GameDetails];
SELECT * FROM [User];
SELECT * FROM [GameType];
SELECT * FROM [GameUsers];


-- scaffold-DbContext "Server = (localdb)\MSSQLLocalDB;Initial Catalog=SportBuddiesDB;User ID=SportBuddiesAdminLogin;Password=thePassword;" Microsoft.EntityFrameworkCore.SqlServer -OutPutDir Models -Context SportBuddiesDbContext -DataAnnotations –force