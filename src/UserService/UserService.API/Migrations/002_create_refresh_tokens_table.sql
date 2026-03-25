IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE RefreshTokens (
        RefreshTokenId INT           NOT NULL IDENTITY(1,1),
        UserId         INT           NOT NULL,
        Token          NVARCHAR(512) NOT NULL,
        ExpiresAt      DATETIME2     NOT NULL,
        IsRevoked      BIT           NOT NULL DEFAULT 0,
        CreatedAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_RefreshTokens PRIMARY KEY (RefreshTokenId),
        CONSTRAINT UQ_RefreshTokens_Token UNIQUE (Token),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
