IF OBJECT_ID(N'dbo.Remittances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Remittances
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_Remittances PRIMARY KEY,
        UserId              UNIQUEIDENTIFIER    NOT NULL,
        SenderName          NVARCHAR(200)       NOT NULL,
        SenderIdType        INT                 NOT NULL,
        SenderIdNumber      NVARCHAR(50)        NOT NULL,
        BeneficiaryName     NVARCHAR(200)       NOT NULL,
        BeneficiaryIdType   INT                 NOT NULL,
        BeneficiaryIdNumber NVARCHAR(50)        NOT NULL,
        Amount              DECIMAL(18,2)       NOT NULL,
        Currency            NVARCHAR(3)         NOT NULL,
        Status              INT                 NOT NULL,
        [Date]              DATE                NOT NULL,
        TrackingCode        NVARCHAR(20)        NOT NULL,
        IsDeleted           BIT                 NOT NULL CONSTRAINT DF_Remittances_IsDeleted DEFAULT (0),
        CreatedAtUtc        DATETIME2(3)        NOT NULL,
        UpdatedAtUtc        DATETIME2(3)        NULL,
        CONSTRAINT FK_Remittances_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX UQ_Remittances_TrackingCode
        ON dbo.Remittances(TrackingCode)
        WHERE IsDeleted = 0;

    CREATE NONCLUSTERED INDEX IX_Remittances_UserId_Date_CreatedAt
        ON dbo.Remittances (UserId, [Date] DESC, CreatedAtUtc DESC)
        WHERE IsDeleted = 0;
END;
GO
