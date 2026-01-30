/* =========================================================
   USERS
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE dbo.Users (
        UserId        BIGINT IDENTITY(1,1) PRIMARY KEY,
        Email         NVARCHAR(256) NOT NULL UNIQUE,
        PasswordHash  NVARCHAR(512) NOT NULL,
        CreatedUtc    DATETIME2 NOT NULL,
        IsActive      BIT NOT NULL
    );
END

/* =========================================================
   ORGANIZATIONS
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Organizations')
BEGIN
    CREATE TABLE dbo.Organizations (
        OrgId      BIGINT IDENTITY(1,1) PRIMARY KEY,
        Name       NVARCHAR(256) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        IsActive   BIT NOT NULL
    );
END

/* =========================================================
   USER ↔ ORG
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserOrganizations')
BEGIN
    CREATE TABLE dbo.UserOrganizations (
        UserId BIGINT NOT NULL,
        OrgId  BIGINT NOT NULL,
        Role   NVARCHAR(64) NOT NULL,
        CONSTRAINT PK_UserOrganizations PRIMARY KEY (UserId, OrgId)
    );
END

/* =========================================================
   REFRESH TOKENS
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        RefreshTokenId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId         BIGINT NOT NULL,
        TokenHash      NVARCHAR(512) NOT NULL,
        ExpiresUtc     DATETIME2 NOT NULL,
        CreatedUtc    DATETIME2 NOT NULL,
        RevokedUtc    DATETIME2 NULL,
        ReplacedByTokenHash NVARCHAR(512) NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId')
BEGIN
    CREATE INDEX IX_RefreshTokens_UserId
    ON dbo.RefreshTokens(UserId);
END

/* =========================================================
   API KEYS
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiKeys')
BEGIN
    CREATE TABLE dbo.ApiKeys (
        ApiKeyId    BIGINT IDENTITY(1,1) PRIMARY KEY,
        OrgId       BIGINT NOT NULL,
        Name        NVARCHAR(128) NOT NULL,
        Prefix      NVARCHAR(32) NOT NULL UNIQUE,
        SecretHash  NVARCHAR(512) NOT NULL,
        ScopesCsv   NVARCHAR(512) NOT NULL,
        IsActive    BIT NOT NULL,
        CreatedUtc  DATETIME2 NOT NULL,
        LastUsedUtc DATETIME2 NULL,
        RevokedUtc  DATETIME2 NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApiKeys_OrgId')
BEGIN
    CREATE INDEX IX_ApiKeys_OrgId
    ON dbo.ApiKeys(OrgId);
END

/* =========================================================
   API USAGE LOGS
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiUsageLogs')
BEGIN
    CREATE TABLE dbo.ApiUsageLogs (
        ApiUsageLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ApiKeyId      BIGINT NULL,
        UserId        BIGINT NULL,
        Method        NVARCHAR(16) NOT NULL,
        Path          NVARCHAR(512) NOT NULL,
        StatusCode    INT NOT NULL,
        DurationMs    INT NOT NULL,
        Ip            NVARCHAR(64) NULL,
        CreatedUtc    DATETIME2 NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApiUsageLogs_ApiKeyId')
BEGIN
    CREATE INDEX IX_ApiUsageLogs_ApiKeyId
    ON dbo.ApiUsageLogs(ApiKeyId);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApiUsageLogs_UserId')
BEGIN
    CREATE INDEX IX_ApiUsageLogs_UserId
    ON dbo.ApiUsageLogs(UserId);
END

/* =========================================================
   API USAGE COUNTERS (QUOTA)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiUsageCounters')
BEGIN
    CREATE TABLE dbo.ApiUsageCounters (
        ApiKeyId     BIGINT NOT NULL,
        DateUtc      DATE NOT NULL,
        RequestCount INT NOT NULL,
        CONSTRAINT PK_ApiUsageCounters PRIMARY KEY (ApiKeyId, DateUtc)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApiUsageCounters_DateUtc')
BEGIN
    CREATE INDEX IX_ApiUsageCounters_DateUtc
    ON dbo.ApiUsageCounters(DateUtc);
END
