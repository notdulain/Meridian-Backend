IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Drivers]') AND type = N'U')
BEGIN
    CREATE TABLE Drivers (
        DriverId INT IDENTITY(1,1) PRIMARY KEY,
        UserId NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        LicenseNumber NVARCHAR(50) NOT NULL UNIQUE,
        LicenseExpiry NVARCHAR(20) NOT NULL,
        PhoneNumber NVARCHAR(20) NOT NULL,
        MaxWorkingHoursPerDay FLOAT NOT NULL DEFAULT 8.0,
        CurrentWorkingHoursToday FLOAT NOT NULL DEFAULT 0.0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
