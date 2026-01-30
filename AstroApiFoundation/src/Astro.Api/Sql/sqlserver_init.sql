/*
  SQL Server (MSSQL) idempotent schema for Astro API Foundation.
  Foreign keys REMOVED.
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
        CONSTRAINT PK_UserOrganizations PRIMARY KEY (UserId, OrgId)
    );
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
        ReplacedByTokenHash NVARCHAR(400) NULL
    );

    CREATE INDEX IX_RefreshTokens_UserId
    ON dbo.RefreshTokens(UserId);
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
        RevokedUtc  DATETIME2(0) NULL
    );

    CREATE INDEX IX_ApiKeys_OrgId
    ON dbo.ApiKeys(OrgId);
END;

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

    CREATE INDEX IX_ApiUsageLogs_ApiKeyId
    ON dbo.ApiUsageLogs(ApiKeyId);

    CREATE INDEX IX_ApiUsageLogs_UserId
    ON dbo.ApiUsageLogs(UserId);
END;

IF OBJECT_ID('dbo.ApiUsageCounters', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiUsageCounters (
        ApiKeyId       BIGINT NOT NULL,
        DateUtc        DATE NOT NULL,
        RequestCount   INT NOT NULL,
        CONSTRAINT PK_ApiUsageCounters
            PRIMARY KEY (ApiKeyId, DateUtc)
    );
END;

CREATE INDEX IX_ApiUsageCounters_DateUtc
ON dbo.ApiUsageCounters (DateUtc);

