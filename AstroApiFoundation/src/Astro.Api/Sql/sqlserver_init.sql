/*
  SQL Server (MSSQL) idempotent schema for Astro API Foundation.
  Run automatically on app start (dev), or run manually in prod.
*/

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Email        NVARCHAR(320) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(400) NOT NULL,
        CreatedUtc   DATETIME2(0) NOT NULL,
        IsActive     BIT NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Organizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Organizations (
        OrgId      BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name       NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL,
        IsActive   BIT NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.UserOrganizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserOrganizations (
        UserId BIGINT NOT NULL,
        OrgId  BIGINT NOT NULL,
        Role   NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_UserOrganizations PRIMARY KEY (UserId, OrgId),
        CONSTRAINT FK_UserOrganizations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserOrganizations_Organizations FOREIGN KEY (OrgId) REFERENCES dbo.Organizations(OrgId)
    );
END;

-- ==========================
-- Roles (multi-role)
-- ==========================
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedUtc DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO dbo.Roles(Code, Name)
    VALUES ('consumer','Consumer'), ('astrologer','Astrologer'), ('admin','Admin');
END;

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserId BIGINT NOT NULL,
        RoleId INT NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy BIGINT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId) ON DELETE CASCADE
    );
END;

-- ==========================
-- User sessions (multi-device refresh)
-- ==========================
IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSessions (
        SessionId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId BIGINT NOT NULL,
        RefreshTokenHash NVARCHAR(200) NOT NULL,
        ExpiresUtc DATETIME2(0) NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL,
        RevokedUtc DATETIME2(0) NULL,
        ReplacedByTokenHash NVARCHAR(200) NULL,
        UserAgent NVARCHAR(400) NULL,
        IpAddress NVARCHAR(64) NULL,
        CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX UX_UserSessions_RefreshTokenHash ON dbo.UserSessions(RefreshTokenHash);
    CREATE INDEX IX_UserSessions_UserId ON dbo.UserSessions(UserId, CreatedUtc DESC);
END;

-- ==========================
-- Marketplace (Astrologers)
-- ==========================
IF OBJECT_ID(N'dbo.AstrologerProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstrologerProfiles (
        AstrologerId BIGINT NOT NULL PRIMARY KEY,
        DisplayName NVARCHAR(120) NOT NULL,
        Bio NVARCHAR(2000) NULL,
        ExperienceYears INT NOT NULL,
        LanguagesCsv NVARCHAR(400) NOT NULL,
        SpecializationsCsv NVARCHAR(400) NOT NULL,
        PricePerMinute DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL,
        VerifiedUtc DATETIME2(0) NULL,
        CONSTRAINT FK_AstrologerProfiles_Users FOREIGN KEY (AstrologerId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );

    CREATE INDEX IX_AstrologerProfiles_Status ON dbo.AstrologerProfiles(Status);
END;

IF OBJECT_ID(N'dbo.AstrologerAvailability', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstrologerAvailability (
        AvailabilityId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AstrologerId BIGINT NOT NULL,
        DayOfWeek INT NOT NULL,
        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL,
        CONSTRAINT FK_AstrologerAvailability_Profiles FOREIGN KEY (AstrologerId) REFERENCES dbo.AstrologerProfiles(AstrologerId) ON DELETE CASCADE
    );

    CREATE INDEX IX_AstrologerAvailability_AstrologerId ON dbo.AstrologerAvailability(AstrologerId);
END;

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        RefreshTokenId      BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId              BIGINT NOT NULL,
        TokenHash           NVARCHAR(400) NOT NULL,
        ExpiresUtc          DATETIME2(0) NOT NULL,
        CreatedUtc          DATETIME2(0) NOT NULL,
        RevokedUtc          DATETIME2(0) NULL,
        ReplacedByTokenHash NVARCHAR(400) NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );

    CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens(UserId);
END;

IF OBJECT_ID(N'dbo.ApiKeys', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiKeys (
        ApiKeyId    BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrgId       BIGINT NOT NULL,
        Name        NVARCHAR(120) NOT NULL,
        Prefix      NVARCHAR(40) NOT NULL UNIQUE,
        SecretHash  NVARCHAR(400) NOT NULL,
        ScopesCsv   NVARCHAR(500) NOT NULL,
        IsActive    BIT NOT NULL,
        CreatedUtc  DATETIME2(0) NOT NULL,
        LastUsedUtc DATETIME2(0) NULL,
        RevokedUtc  DATETIME2(0) NULL,
        DailyQuota  INT NULL,
        PlanCode    NVARCHAR(40) NULL,
        CONSTRAINT FK_ApiKeys_Organizations FOREIGN KEY (OrgId) REFERENCES dbo.Organizations(OrgId)
    );

    CREATE INDEX IX_ApiKeys_OrgId ON dbo.ApiKeys(OrgId);
END;

-- Add new columns if upgrading an existing db
IF COL_LENGTH('dbo.ApiKeys','DailyQuota') IS NULL
    ALTER TABLE dbo.ApiKeys ADD DailyQuota INT NULL;
IF COL_LENGTH('dbo.ApiKeys','PlanCode') IS NULL
    ALTER TABLE dbo.ApiKeys ADD PlanCode NVARCHAR(40) NULL;

IF OBJECT_ID(N'dbo.ApiUsageLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiUsageLogs (
        ApiUsageLogId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ApiKeyId      BIGINT NULL,
        UserId        BIGINT NULL,
        Method        NVARCHAR(10) NOT NULL,
        Path          NVARCHAR(500) NOT NULL,
        StatusCode    INT NOT NULL,
        DurationMs    INT NOT NULL,
        Ip            NVARCHAR(80) NULL,
        CreatedUtc    DATETIME2(0) NOT NULL,
        CONSTRAINT FK_ApiUsageLogs_ApiKeys FOREIGN KEY (ApiKeyId) REFERENCES dbo.ApiKeys(ApiKeyId),
        CONSTRAINT FK_ApiUsageLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );

    CREATE INDEX IX_ApiUsageLogs_ApiKeyId ON dbo.ApiUsageLogs(ApiKeyId);
    CREATE INDEX IX_ApiUsageLogs_UserId ON dbo.ApiUsageLogs(UserId);
END;

IF OBJECT_ID(N'dbo.ApiUsageCounters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiUsageCounters (
        ApiKeyId      BIGINT NOT NULL,
        DateUtc       DATETIME2(0) NOT NULL,
        RequestCount  INT NOT NULL,
        CONSTRAINT PK_ApiUsageCounters PRIMARY KEY (ApiKeyId, DateUtc),
        CONSTRAINT FK_ApiUsageCounters_ApiKeys FOREIGN KEY (ApiKeyId) REFERENCES dbo.ApiKeys(ApiKeyId)
    );
END;
