CREATE TABLE Vehicles (
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    LicensePlate NVARCHAR(20) UNIQUE NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    CapacityWeightKg DECIMAL(10, 2) NOT NULL CHECK (CapacityWeightKg > 0),
    CapacityVolumeM3 DECIMAL(10, 2) NOT NULL CHECK (CapacityVolumeM3 > 0),
    FuelEfficiencyKmPerL DECIMAL(5, 2) NOT NULL CHECK (FuelEfficiencyKmPerL > 0),
    Status NVARCHAR(50) DEFAULT 'Available',
    CurrentDriverId INT NULL,
    CreatedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_vehicles_status ON Vehicles(Status);
CREATE INDEX idx_license ON Vehicles(LicensePlate);
