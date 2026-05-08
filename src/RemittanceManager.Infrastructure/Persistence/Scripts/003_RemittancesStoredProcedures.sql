CREATE OR ALTER PROCEDURE dbo.sp_Remittances_Insert
    @Id                  UNIQUEIDENTIFIER,
    @UserId              UNIQUEIDENTIFIER,
    @SenderName          NVARCHAR(200),
    @SenderIdType        INT,
    @SenderIdNumber      NVARCHAR(50),
    @BeneficiaryName     NVARCHAR(200),
    @BeneficiaryIdType   INT,
    @BeneficiaryIdNumber NVARCHAR(50),
    @Amount              DECIMAL(18,2),
    @Currency            NVARCHAR(3),
    @Status              INT,
    @Date                DATE,
    @CreatedAtUtc        DATETIME2(3),
    @TrackingCode        NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Remittances
        (Id, UserId, SenderName, SenderIdType, SenderIdNumber,
         BeneficiaryName, BeneficiaryIdType, BeneficiaryIdNumber,
         Amount, Currency, Status, [Date], TrackingCode, CreatedAtUtc)
    VALUES
        (@Id, @UserId, @SenderName, @SenderIdType, @SenderIdNumber,
         @BeneficiaryName, @BeneficiaryIdType, @BeneficiaryIdNumber,
         @Amount, @Currency, @Status, @Date, @TrackingCode, @CreatedAtUtc);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Remittances_ListByUser
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserId, BeneficiaryName, Amount, Currency, Status, [Date], CreatedAtUtc, TrackingCode,
           SenderName, SenderIdType, SenderIdNumber, BeneficiaryIdType, BeneficiaryIdNumber, UpdatedAtUtc
    FROM dbo.Remittances
    WHERE UserId = @UserId AND IsDeleted = 0
    ORDER BY [Date] DESC, CreatedAtUtc DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Remittances_SoftDelete
    @Id     UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Remittances
    SET IsDeleted = 1
    WHERE Id = @Id AND UserId = @UserId AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO
