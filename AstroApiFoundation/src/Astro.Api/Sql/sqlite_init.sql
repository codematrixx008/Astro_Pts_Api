PRAGMA foreign_keys = OFF;

CREATE TABLE IF NOT EXISTS Users (
    UserId        INTEGER PRIMARY KEY AUTOINCREMENT,
    Email         TEXT NOT NULL UNIQUE,
    PasswordHash  TEXT NOT NULL,
    CreatedUtc    TEXT NOT NULL,
    IsActive      INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Organizations (
    OrgId      INTEGER PRIMARY KEY AUTOINCREMENT,
    Name       TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    IsActive   INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS UserOrganizations (
    UserId INTEGER NOT NULL,
    OrgId  INTEGER NOT NULL,
    Role   TEXT NOT NULL,
    PRIMARY KEY (UserId, OrgId)
);

CREATE TABLE IF NOT EXISTS RefreshTokens (
    RefreshTokenId      INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId              INTEGER NOT NULL,
    TokenHash           TEXT NOT NULL,
    ExpiresUtc          TEXT NOT NULL,
    CreatedUtc          TEXT NOT NULL,
    RevokedUtc          TEXT NULL,
    ReplacedByTokenHash TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_RefreshTokens_UserId
ON RefreshTokens(UserId);

CREATE TABLE IF NOT EXISTS ApiKeys (
    ApiKeyId    INTEGER PRIMARY KEY AUTOINCREMENT,
    OrgId       INTEGER NOT NULL,
    Name        TEXT NOT NULL,
    Prefix      TEXT NOT NULL UNIQUE,
    SecretHash  TEXT NOT NULL,
    ScopesCsv   TEXT NOT NULL,
    IsActive    INTEGER NOT NULL,
    CreatedUtc  TEXT NOT NULL,
    LastUsedUtc TEXT NULL,
    RevokedUtc  TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_ApiKeys_OrgId
ON ApiKeys(OrgId);

CREATE TABLE IF NOT EXISTS ApiUsageLogs (
    ApiUsageLogId INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiKeyId      INTEGER NULL,
    UserId        INTEGER NULL,
    Method        TEXT NOT NULL,
    Path          TEXT NOT NULL,
    StatusCode    INTEGER NOT NULL,
    DurationMs    INTEGER NOT NULL,
    Ip            TEXT NULL,
    CreatedUtc    TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_ApiUsageLogs_ApiKeyId
ON ApiUsageLogs(ApiKeyId);

CREATE INDEX IF NOT EXISTS IX_ApiUsageLogs_UserId
ON ApiUsageLogs(UserId);

CREATE TABLE IF NOT EXISTS ApiUsageCounters (
    ApiKeyId      INTEGER NOT NULL,
    DateUtc       TEXT NOT NULL,     -- YYYY-MM-DD (UTC)
    RequestCount  INTEGER NOT NULL,
    PRIMARY KEY (ApiKeyId, DateUtc)
);

CREATE INDEX IF NOT EXISTS IX_ApiUsageCounters_DateUtc
ON ApiUsageCounters (DateUtc);



