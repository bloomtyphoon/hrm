-- =============================================
-- Create DataScopeLevels reference table
-- Source of truth for all available scope levels.
-- Application loads at startup and registers dynamic levels.
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DataScopeLevels' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].DataScopeLevels
    (
        Id              INT             NOT NULL,
        Name            NVARCHAR(50)    NOT NULL,
        DisplayName     NVARCHAR(100)   NOT NULL,
        Category        NVARCHAR(20)    NOT NULL,       -- 'None', 'Global', 'Set', 'Dimension'
        SortOrder       INT             NOT NULL,
        IsActive        BIT             NOT NULL DEFAULT 1,
        DimensionKey    NVARCHAR(50)    NULL,           -- For Dimension category: column key matching [ScopeDimension("key")]
        ResolutionKey   NVARCHAR(50)    NULL,           -- For Set category: resolver strategy key

        CONSTRAINT PK_Identity_DataScopeLevels PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Identity_DataScopeLevels_Name UNIQUE (Name),
        CONSTRAINT CK_Identity_DataScopeLevels_Category CHECK (Category IN ('None', 'Global', 'Set', 'Dimension')),
        CONSTRAINT CK_Identity_DataScopeLevels_DimensionKey CHECK (
            (Category = 'Dimension' AND DimensionKey IS NOT NULL)
            OR (Category <> 'Dimension' AND DimensionKey IS NULL)
        ),
        CONSTRAINT CK_Identity_DataScopeLevels_ResolutionKey CHECK (
            (Category = 'Set' AND ResolutionKey IS NOT NULL)
            OR (Category <> 'Set' AND ResolutionKey IS NULL)
        )
    );

    PRINT 'Created table [Identity].DataScopeLevels';
END
GO

-- =============================================
-- Seed well-known scope levels
-- =============================================

-- Use MERGE for idempotent seeding
MERGE INTO [Identity].DataScopeLevels AS target
USING (VALUES
    (0, N'None',          N'Không có quyền',      'None',      0, 1, NULL,         NULL),
    (1, N'Self',          N'Chỉ bản thân',        'Set',       1, 1, NULL,         'Self'),
    (2, N'DirectReports', N'Cấp dưới trực tiếp',  'Set',       2, 1, NULL,         'DirectReports'),
    (3, N'EmployeeSet',   N'Toàn bộ cấp dưới',    'Set',       3, 1, NULL,         'AllSubordinates'),
    (4, N'Position',      N'Cùng chức danh',       'Dimension', 4, 1, 'Position',   NULL),
    (5, N'Department',    N'Cùng phòng ban',       'Dimension', 5, 1, 'Department', NULL),
    (6, N'Company',       N'Toàn công ty',         'Dimension', 6, 1, 'Company',    NULL),
    (7, N'Country',       N'Cùng quốc gia',       'Dimension', 7, 1, 'Country',    NULL),
    (8, N'Region',        N'Cùng khu vực',        'Dimension', 8, 1, 'Region',     NULL),
    (9, N'Global',        N'Toàn hệ thống',       'Global',    9, 1, NULL,         NULL)
) AS source (Id, Name, DisplayName, Category, SortOrder, IsActive, DimensionKey, ResolutionKey)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        DisplayName = source.DisplayName,
        Category = source.Category,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        DimensionKey = source.DimensionKey,
        ResolutionKey = source.ResolutionKey
WHEN NOT MATCHED THEN
    INSERT (Id, Name, DisplayName, Category, SortOrder, IsActive, DimensionKey, ResolutionKey)
    VALUES (source.Id, source.Name, source.DisplayName, source.Category, source.SortOrder, source.IsActive, source.DimensionKey, source.ResolutionKey);

PRINT 'Seeded [Identity].DataScopeLevels with well-known scope levels';
GO

-- =============================================
-- Add FK from RolePermissions.Scope to DataScopeLevels.Id
-- =============================================

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Identity_RolePermissions_DataScopeLevels'
)
BEGIN
    ALTER TABLE [Identity].RolePermissions
    ADD CONSTRAINT FK_Identity_RolePermissions_DataScopeLevels
        FOREIGN KEY (Scope) REFERENCES [Identity].DataScopeLevels (Id);

    PRINT 'Added FK: RolePermissions.Scope -> DataScopeLevels.Id';
END
GO

-- =============================================
-- Add FK from EmployeeProfiles.DefaultScopeLevel to DataScopeLevels.Id
-- =============================================

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Identity_EmployeeProfiles_DataScopeLevels'
)
BEGIN
    ALTER TABLE [Identity].EmployeeProfiles
    ADD CONSTRAINT FK_Identity_EmployeeProfiles_DataScopeLevels
        FOREIGN KEY (DefaultScopeLevel) REFERENCES [Identity].DataScopeLevels (Id);

    PRINT 'Added FK: EmployeeProfiles.DefaultScopeLevel -> DataScopeLevels.Id';
END
GO
