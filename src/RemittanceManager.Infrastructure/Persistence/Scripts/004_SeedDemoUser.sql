-- 004_SeedDemoUser.sql
-- Seed demo user and sample remittances for RemittanceManager

-- Remove demo remittances if they exist
DELETE FROM [Remittances]
WHERE Id IN (
    '10000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000003'
);

-- Remove demo user if exists
DELETE FROM [Users]
WHERE Id = '00000000-0000-0000-0000-000000000001'
   OR Email = 'demo@remittance.test';

-- Insert demo user
INSERT INTO [Users] (Id, Email, PasswordHash, DisplayName, CreatedAtUtc)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'demo@remittance.test',
    '$2a$12$fMwchnV9Mn.HhDegHUR0mO5bV7QhgsFtnuFxKctKOs8LdMNXYYF/q', -- Example hash for 'Demo123!'
    'Demo User',
    GETUTCDATE()
);

INSERT INTO [Remittances] (
    Id,
    UserId,
    SenderName,
    SenderIdType,
    SenderIdNumber,
    BeneficiaryName,
    BeneficiaryIdType,
    BeneficiaryIdNumber,
    Amount,
    Currency,
    Status,
    Date,
    TrackingCode,
    CreatedAtUtc
)
VALUES
('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Demo User', 0, '1234567890', 'Maria Castro', 3, '12345678', 250.00, 'USD', 2, GETUTCDATE(), 'RM-DEMO01', GETUTCDATE()),
('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'Demo User', 0, '1234567890', 'Carlos Perez', 3, '23456789', 120.50, 'USD', 1, GETUTCDATE(), 'RM-DEMO02', GETUTCDATE()),
('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'Demo User', 0, '1234567890', 'Ana Rodriguez', 3, '34567890', 80.00, 'EUR', 0, GETUTCDATE(), 'RM-DEMO03', GETUTCDATE());
