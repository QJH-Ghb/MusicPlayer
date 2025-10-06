-- �q���ƪ�
IF OBJECT_ID(N'dbo.Singer', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Singer (
        SingerID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Singer PRIMARY KEY,
        SingerName NVARCHAR(100) NOT NULL,   -- �q��W��
        Album      NVARCHAR(100) NULL        -- �M��
    );
END;
GO

-- �Τ��ƪ�
IF OBJECT_ID(N'dbo.[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Users] (
        UserID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        UserName     NVARCHAR(50)  NOT NULL,   -- �Τ�W��
        UserPassword NVARCHAR(256) NOT NULL    -- �Τ�K�X�]��ĳ�s����^
    );
END;
GO

-- �q���M���ƪ�
IF OBJECT_ID(N'dbo.SongList', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SongList (
        SongID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SongList PRIMARY KEY,
        SingerID  INT NOT NULL,                -- �~�� �� Singer(SingerID)
        UserID    INT NOT NULL,                -- �~�� �� Users(UserID)
        SongTitle NVARCHAR(100) NOT NULL       -- �q���W�١]�i�̻ݨD�O�d/�����^
    );
END;
GO

-- �Y�~��|���إߡA�h�إߥ~�����
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
