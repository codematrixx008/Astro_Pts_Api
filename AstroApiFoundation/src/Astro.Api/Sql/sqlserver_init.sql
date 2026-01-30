/*
  SQL Server (MSSQL) idempotent schema for Astro API Foundation.
  Foreign keys REMOVED.
*/

/* =========================================================
   USERS
   ========================================================= */
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

/* =========================================================
   ORGANIZATIONS
   ========================================================= */
IF OBJECT_ID(N'dbo.Organizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Organizations (
        OrgId      BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name       NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL,
        IsActive   BIT NOT NULL
    );
END;

/* =========================================================
   USER ↔ ORG
   ========================================================= */
IF OBJECT_ID(N'dbo.UserOrganizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserOrganizations (
        UserId BIGINT NOT NULL,
        OrgId  BIGINT NOT NULL,
        Role   NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_UserOrganizations PRIMARY KEY (UserId, OrgId)
    );
END;

/* =========================================================
   REFRESH TOKENS
   ========================================================= */
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        RefreshTokenId      BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId              BIGINT NOT NULL,
        TokenHash           NVARCHAR(400) NOT NULL,
        ExpiresUtc          DATETIME2(0) NOT NULL,
        CreatedUtc          DATETIME2(0) NOT NULL,
        RevokedUtc          DATETIME2(0) NULL,
        ReplacedByTokenHash NVARCHAR(400) NULL
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RefreshTokens_UserId'
      AND object_id = OBJECT_ID('dbo.RefreshTokens')
)
BEGIN
    CREATE INDEX IX_RefreshTokens_UserId
    ON dbo.RefreshTokens(UserId);
END;

/* =========================================================
   API KEYS
   ========================================================= */
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
        RevokedUtc  DATETIME2(0) NULL
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApiKeys_OrgId'
      AND object_id = OBJECT_ID('dbo.ApiKeys')
)
BEGIN
    CREATE INDEX IX_ApiKeys_OrgId
    ON dbo.ApiKeys(OrgId);
END;

/* =========================================================
   API USAGE LOGS
   ========================================================= */
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
        CreatedUtc    DATETIME2(0) NOT NULL
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApiUsageLogs_ApiKeyId'
      AND object_id = OBJECT_ID('dbo.ApiUsageLogs')
)
BEGIN
    CREATE INDEX IX_ApiUsageLogs_ApiKeyId
    ON dbo.ApiUsageLogs(ApiKeyId);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApiUsageLogs_UserId'
      AND object_id = OBJECT_ID('dbo.ApiUsageLogs')
)
BEGIN
    CREATE INDEX IX_ApiUsageLogs_UserId
    ON dbo.ApiUsageLogs(UserId);
END;

/* =========================================================
   API USAGE COUNTERS (QUOTA)
   ========================================================= */
IF OBJECT_ID(N'dbo.ApiUsageCounters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiUsageCounters (
        ApiKeyId     BIGINT NOT NULL,
        DateUtc      DATE NOT NULL,
        RequestCount INT NOT NULL,
        CONSTRAINT PK_ApiUsageCounters
            PRIMARY KEY (ApiKeyId, DateUtc)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApiUsageCounters_DateUtc'
      AND object_id = OBJECT_ID('dbo.ApiUsageCounters')
)
BEGIN
    CREATE INDEX IX_ApiUsageCounters_DateUtc
    ON dbo.ApiUsageCounters(DateUtc);
END;


-- Astrologer profile
IF OBJECT_ID('dbo.AstrologerProfiles','U') IS NULL
BEGIN
  CREATE TABLE dbo.AstrologerProfiles (
    AstrologerId BIGINT NOT NULL PRIMARY KEY,  -- logical link to Users(UserId)
    DisplayName NVARCHAR(200) NOT NULL,
    Bio NVARCHAR(MAX) NULL,
    ExperienceYears INT NULL,
    LanguagesCsv NVARCHAR(200) NULL,
    SpecializationsCsv NVARCHAR(200) NULL,
    PricePerMinute DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,              -- applied, verified, active, suspended
    CreatedUtc DATETIME2 NOT NULL,
    VerifiedUtc DATETIME2 NULL
  );
END


-- Availability (NO FK)
IF OBJECT_ID('dbo.AstrologerAvailability','U') IS NULL
BEGIN
  CREATE TABLE dbo.AstrologerAvailability (
    AvailabilityId BIGINT IDENTITY PRIMARY KEY,
    AstrologerId BIGINT NOT NULL,
    DayOfWeek INT NOT NULL,                    -- 0=Sun..6=Sat
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedUtc DATETIME2 NOT NULL
  );
END


-- Chat sessions
IF OBJECT_ID('dbo.ChatSessions','U') IS NULL
BEGIN
  CREATE TABLE dbo.ChatSessions (
    ChatSessionId BIGINT IDENTITY PRIMARY KEY,
    ConsumerId BIGINT NOT NULL,
    AstrologerId BIGINT NOT NULL,
    Status NVARCHAR(50) NOT NULL,              -- requested, accepted, active, ended, cancelled
    RequestedUtc DATETIME2 NOT NULL,
    AcceptedUtc DATETIME2 NULL,
    StartedUtc DATETIME2 NULL,
    EndedUtc DATETIME2 NULL,
    RatePerMinute DECIMAL(10,2) NOT NULL,
    Topic NVARCHAR(200) NULL
  );
END


-- Chat messages (NO FK, NO CASCADE)
IF OBJECT_ID('dbo.ChatMessages','U') IS NULL
BEGIN
  CREATE TABLE dbo.ChatMessages (
    MessageId BIGINT IDENTITY PRIMARY KEY,
    ChatSessionId BIGINT NOT NULL,
    SenderUserId BIGINT NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
  );

  CREATE INDEX IX_ChatMessages_SessionCreated
    ON dbo.ChatMessages(ChatSessionId, CreatedUtc);
END
