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
	pic			VARCHAR(30)	NOT NULL,
	title		VARCHAR(30) NOT NULL,
	genre		VARCHAR(30) NOT NULL,
	releaseDate	INT			NOT NULL,
	developer	VARCHAR(30) NOT NULL,
	console		VARCHAR(30) NOT NULL,
	rating		FLOAT		NOT NULL
)
GO

/*** Users Table ***/
CREATE TABLE Users (
	userId		INT				PRIMARY KEY IDENTITY,
	email		VARCHAR(30)		NOT NULL,
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
	comment		VARCHAR(30)		NOT NULL	
)
GO

-- INSERTS
/*** Games Insert(building the entries in the table) ***/
INSERT INTO Games(pic, title, genre, releaseDate, developer, console, rating) VALUES 
('superbro1.jpg', 'Super Mario Bros', 'Plumbing', 1985, 'Nintendo', 'NES', 9.9),
('plumbing.jpg', 'Super Mario Bros 2', 'Plumbing', 1987, 'Nintendo', 'NES', 9.0),
('toilet.jpg','Super Mario Bros 3', 'Plumbing', 1989, 'Nintendo', 'NES', 8.4),
('reet.png','Super Smash Bros', 'Wrestling', 1995, 'Nintendo', 'GameCube', 9.9),
('pikachu.jpg','Pokemon Bros: Melee', 'Friendship', 1999, 'Nintendo', 'Xbox', 6.9),
('yellow.png','Banana Bros', 'Plumbing', 1985, 'Nintendo', 'NES', 9),
('plumbing2.jpg','Super Hoes', 'Sisterhood', 2005, 'Nintendo', 'Xbox 360', 2.9),
('plumbing3.jpg','Super Bros', 'Frat', 2006, 'Fratendo', 'Xbox 360', 9.9),
('plumbing4.jpg','Mario', 'Mystery', 2007, 'Nintendo', 'Playstation 1', 4.2),
('plumbing5.jpg','Luigi', 'Murder', 2010, 'Nintendo', 'Playstation 3', 9.9),
('plumbing6.jpg','Peach Fights Back', 'Woke', 2015, 'Nintendo', 'Playstation 3.5', 0)
GO

/***  Users Insert ***/
INSERT INTO Users(email, passwd, admin) VALUES
('bigBrother@gmail.com', '1984', 1),
('litleBrother@gmail.com', '1990s', 0)
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
	WHERE title LIKE @title
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
-- select * from Games
-- select * from Comments
-- select * from Users