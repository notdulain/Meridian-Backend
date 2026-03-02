CREATE TABLE Users (
    UserId       INT           NOT NULL IDENTITY(1,1),
    FullName     NVARCHAR(255) NOT NULL,
    Email        NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
