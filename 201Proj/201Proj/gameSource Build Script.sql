USE Master
GO

/*** Object: Database GameSource ***/
IF DB_ID('GameSource') IS NOT NULL
	DROP DATABASE GameSource
GO

CREATE DATABASE GameSource
GO

USE GameSource
GO

/*** Games table ***/
CREATE TABLE Games (
	gameId		INT			PRIMARY KEY IDENTITY,
	pic			VARCHAR(50)	NOT NULL,
	title		VARCHAR(50) NOT NULL,
	genre		VARCHAR(50) NOT NULL,
	releaseDate	INT			NOT NULL,
	developer	VARCHAR(50) NOT NULL,
	console		VARCHAR(50) NOT NULL,
	rating		FLOAT		NOT NULL
)
GO

/*** Users Table ***/
CREATE TABLE Users (
	userId		INT				PRIMARY KEY IDENTITY,
	email		VARCHAR(50)		NOT NULL,
	passwd		VARCHAR(255)	NOT NULL,
	-- 0 is non-admin and 1 is admin
	admin		BIT				NOT NULL
)
GO

/*** Comments table ***/
CREATE TABLE Comments (
	commentId	INT				PRIMARY KEY IDENTITY,
	gameId		INT				NOT NULL,
	userId		INT				NOT NULL,
	comment		VARCHAR(50)		NOT NULL	
)
GO

-- INSERTS
/*** Games Insert(building the entries in the table) ***/
INSERT INTO Games(pic, title, genre, releaseDate, developer, console, rating) VALUES 
('supmarbro1.jpg', 'Mario Bros', 'Platforming', 1983, 'Nintendo', 'NES', 7.5),
('KirbyDreamLand.jpg', 'Kirbys Dream Land', 'Action, platformer', 1992, 'Nintendo', 'GBA', 9.5),
('lol.jpg','League of Legends', 'MOBA', 2009, 'Riot Games', 'PC', 10),
('animalCross.jpg','Animal Crossing: New Leaf', 'Social Simulation Game', 2012, 'Nintendo', 'Nintendo 3DS', 9.6),
('PokemonXY.jpg','PokemonX/Y', 'Role-playing', 2013, 'Nintendo', '3DS', 9.9),
('shovKnight.jpg','Shovel Knight', 'Platform Game', 2014, 'Nintendo', 'Nintendo 3DS', 9.8),
('splatoon.jpg','Splatoon', 'Third-person shooter', 2015, 'Nintendo', 'Wii U', 9.3),
('TheLegendOfZelda.jpg','The Legend Of Zelda: Breath of the Wild', 'Action-adventure', 2017, 'Nintendo', 'Nintendo Switch', 9.6),
('letsgoPika.jpg','Pokemon: Lets Go, Pickachu!', 'Action role-playing', 2018, 'Nintendo', 'Nintendo Switch', 9.3),
('marvUltAll3.jpg','Marvel Ultimate Alliance 3: The Black Order', 'Action role-playing', 2019, 'Nintendo', 'Nintendo Switch', 7.8),
('pokeSwordShield.jpg','Pokemon Sword and Shield', 'Role-playing', 2019, 'Nintendo', 'Nintendo Switch', 9.0)
GO

/***  Users Insert ***/
INSERT INTO Users(email, passwd, admin) VALUES
('bigBrother@gmail.com', '1984', 1),
('littleBrother@gmail.com', '1990s', 0)
GO

/*** Comments Insert ***/
-- first #: references the gameId in table (Games)
-- second #: references the userId in table (Users)
INSERT INTO Comments(gameId, userId, comment) VALUES
(1, 1, 'Worst game ever'),
(6, 2, 'PLayed it all night long')
GO

/*** Game info stored: title, genre,
release date, developer, console on
game rating (by major groups), comments ***/

-- add and delete procedures

/*** Procedure: getGameByTitle ***/
CREATE PROCEDURE getGameByTitle
	@title	VARCHAR(30)

AS
	SELECT * FROM Games
	WHERE title LIKE '%' + @title  + '%'
	ORDER BY title
GO

CREATE PROCEDURE getCommentsByGame
	@gameId	INT
AS
	SELECT * FROM Games
	WHERE gameId = @gameId
GO

CREATE PROCEDURE getAllGames
AS
	SELECT * FROM Games
	ORDER BY title
GO

--pic, title, genre, releaseDate, developer, console, rating
CREATE PROCEDURE addGame
	@pic			VARCHAR(30),
	@title			VARCHAR(30),
	@genre			VARCHAR(30),
	@releaseDate	INT,
	@developer		VARCHAR(30),
	@console		VARCHAR(30),
	@rating			FLOAT
AS
	INSERT INTO Games(pic, title, genre, releaseDate, developer, console, rating) VALUES
	(@pic, @title, @genre, @releaseDate, @developer, @console, @rating)
GO

CREATE PROCEDURE deleteGame
	@gameId			INT
AS
	DELETE FROM Games
	WHERE gameId = @gameId
GO

/**need to get the user and check if user in table, and if password and user match**/
CREATE PROCEDURE getUser
	@email VARCHAR(50),
	@passwd VARCHAR(50)
AS
	SELECT * FROM Users
	WHERE email =  @email  AND passwd = @passwd
GO

-- Need to add user
CREATE PROCEDURE addUser
	@email VARCHAR(50),
	@passwd VARCHAR(50)
AS
	INSERT INTO Users(email, passwd) VALUES
	(@email, @passwd)
GO

--ForeignKey references

-- GameId refrence
ALTER TABLE Comments
WITH NOCHECK ADD CONSTRAINT FK_Comments_Games
FOREIGN KEY (gameId)
REFERENCES Games (gameId)
ON UPDATE CASCADE
GO

-- UserId reference
ALTER TABLE Comments
WITH NOCHECK ADD CONSTRAINT FK_Comments_Users
FOREIGN KEY (userId)
REFERENCES Users (userId)
ON UPDATE CASCADE
GO

-- if we want to see current table
-- DON'T USE VIEW DATA
-- SELECT * from Games
-- SELECT * from Comments
-- SELECT * from Users
-- Tests
-- SELECT pic FROM Games;
-- SELECT title FROM Games;
-- SELECT genre FROM Games;
-- SELECT releaseDate FROM Games;
-- SELECT developer FROM Games;
-- SELECT console FROM Games;
-- SELECT rating FROM Games;
-- SELECT * FROM Comments WHERE gameID='1';
-- SELECT * FROM Comments WHERE userId='1';
-- SELECT * From Comments WHERE comment LIKE 'worst%';

--exec sp_configure 'clr enabled', 1;
--reconfigure
--go

EXEC getUser 'littleBrother@gmail.com', '1990s'