IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Email           NVARCHAR(256)       NOT NULL,
        PasswordHash    NVARCHAR(200)       NOT NULL,
        DisplayName     NVARCHAR(100)       NOT NULL,
        CreatedAtUtc    DATETIME2(3)        NOT NULL
    );

    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
END;
GO
