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
('MarioBros.jpg', 'Mario Bros', 'Platforming', 1983, 'Nintendo', 'NES', 7.5),
('KirbyDreamLand.jpg', 'Kirbys Dream Land', 'Action, platformer', 1992, 'Nintendo', 'GBA', 9.5),
('PokemonXY.jpg','PokemonX/Y', 'Role-playing', 2013, 'Nintendo', '3DS', 9.9),
('TheLegendOfZelda.jpg','The Legend Of Zelda: Breath of the Wild', 'Action-adventure', 2017, 'Nintendo', 'Switch', 9.6),
('lol.jpg','League of Legends', 'MOBA', 2009, 'Riot Games', 'PC', 10),
('yellow.png','Banana Bros', 'Plumbing', 1985, 'Nintendo', 'NES', 9),
('plumbing2.jpg','Super Hoes', 'Sisterhood', 2005, 'Nintendo', 'Xbox 360', 2.9),
('plumbing3.jpg','Super Bros', 'Frat', 2006, 'Fratendo', 'Xbox 360', 9.9),
('plumbing4.jpg','Mario', 'Mystery', 2007, 'Nintendo', 'Playstation 1', 4.2),
('marvUltAll3.jpg','Marvel Ultimate Alliance 3: The Black Order', 'Action role-playing', 2019, 'Nintendo', 'Nintendo Switch', 7.8),
('pokeSwordShield.jpg','Pokemon Sword and Shield', 'Role-playing', 2019, 'Nintendo', 'Nintendo Switch', 9.0)
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
