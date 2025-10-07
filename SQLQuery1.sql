-- 歌手資料表
IF OBJECT_ID(N'dbo.Singer', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Singer (
        SingerID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Singer PRIMARY KEY,
        SingerName NVARCHAR(100) NOT NULL,   -- 歌手名稱
        Album      NVARCHAR(100) NULL        -- 專輯
    );
END;
GO

-- 用戶資料表
IF OBJECT_ID(N'dbo.[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Users] (
        UserID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        UserName     NVARCHAR(50)  NOT NULL,   -- 用戶名稱
        UserPassword NVARCHAR(256) NOT NULL    -- 用戶密碼（建議存雜湊）
    );
END;
GO

-- 歌曲清單資料表
IF OBJECT_ID(N'dbo.SongList', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SongList (
        SongID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SongList PRIMARY KEY,
        SingerID  INT NOT NULL,                -- 外鍵 → Singer(SingerID)
        UserID    INT NOT NULL,                -- 外鍵 → Users(UserID)
        SongTitle NVARCHAR(100) NOT NULL       -- 歌曲名稱（可依需求保留/移除）
    );
END;
GO

-- 若外鍵尚未建立，則建立外鍵約束
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SongList_Singer')
BEGIN
    ALTER TABLE dbo.SongList
    ADD CONSTRAINT FK_SongList_Singer
        FOREIGN KEY (SingerID) REFERENCES dbo.Singer(SingerID);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SongList_Users')
BEGIN
    ALTER TABLE dbo.SongList
    ADD CONSTRAINT FK_SongList_Users
        FOREIGN KEY (UserID) REFERENCES dbo.[Users](UserID);
END;
GO
