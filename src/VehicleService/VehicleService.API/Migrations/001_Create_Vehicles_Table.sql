IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Vehicles]') AND type in (N'U'))
BEGIN
    CREATE TABLE Vehicles (
        VehicleId INT IDENTITY(1,1) PRIMARY KEY,
        PlateNumber NVARCHAR(50) NOT NULL UNIQUE,
        Make NVARCHAR(100) NOT NULL,
        Model NVARCHAR(100) NOT NULL,
        CurrentLocation NVARCHAR(255) NOT NULL DEFAULT '',
        Year INT NOT NULL,
        CapacityKg FLOAT NOT NULL,
        CapacityM3 FLOAT NOT NULL,
        FuelEfficiencyKmPerLitre FLOAT NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Available',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
