-- =============================================
-- Script: Create RefreshTokens Table
-- Module: Identity
-- Purpose: Create RefreshTokens table for JWT session management
-- Dependencies: 001_CreateAccountsTable.sql
-- =============================================

-- Create RefreshTokens table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RefreshTokens' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].RefreshTokens
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Account reference (polymorphic: AccountType + AccountId)
        AccountType TINYINT NOT NULL,       -- 0=System, 1=Employee
        AccountId UNIQUEIDENTIFIER NOT NULL, -- References [Identity].Accounts.Id

        -- Token Information
        Token NVARCHAR(200) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,

        -- Revocation Tracking
        RevokedAt DATETIME2 NULL,
        RevokedByIp NVARCHAR(50) NULL,
        ReplacedByToken NVARCHAR(200) NULL,

        -- Session Tracking
        CreatedByIp NVARCHAR(50) NOT NULL,
        UserAgent NVARCHAR(500) NULL,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Soft Delete
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,

        -- Constraints
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_RefreshTokens_Token UNIQUE (Token),
        CONSTRAINT CK_RefreshTokens_AccountType CHECK (AccountType IN (0, 1))
    )

    PRINT 'Table [Identity].[RefreshTokens] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].[RefreshTokens] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: Active sessions query (most important)
-- Optimized for: WHERE AccountType = @type AND AccountId = @id AND RevokedAt IS NULL
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_Account_Active' AND object_id = OBJECT_ID('[Identity].RefreshTokens'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_Account_Active
    ON [Identity].RefreshTokens (AccountType, AccountId, ExpiresAt)
    INCLUDE (Token, CreatedAtUtc, UserAgent, CreatedByIp)
    WHERE RevokedAt IS NULL

    PRINT 'Index IX_RefreshTokens_Account_Active created'
END
GO

-- Index 2: Token cleanup (expired tokens)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_ExpiresAt' AND object_id = OBJECT_ID('[Identity].RefreshTokens'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_ExpiresAt
    ON [Identity].RefreshTokens (ExpiresAt)
    WHERE RevokedAt IS NULL

    PRINT 'Index IX_RefreshTokens_ExpiresAt created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'RefreshTokens table - JWT session management with polymorphic account reference',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'RefreshTokens'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Account type: 0=System, 1=Employee. Used as discriminator for polymorphic association.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'RefreshTokens',
    @level2type = N'COLUMN', @level2name = N'AccountType'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Account ID - references [Identity].Accounts.Id',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'RefreshTokens',
    @level2type = N'COLUMN', @level2name = N'AccountId'
GO

PRINT 'Script 002_CreateRefreshTokensTable.sql completed'
GO
