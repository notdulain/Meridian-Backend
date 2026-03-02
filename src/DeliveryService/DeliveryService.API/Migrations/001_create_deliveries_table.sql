CREATE TABLE Deliveries (
    DeliveryId INT IDENTITY(1,1) PRIMARY KEY,
    PickupAddress NVARCHAR(500) NOT NULL,
    DeliveryAddress NVARCHAR(500) NOT NULL,
    PackageWeightKg DECIMAL(10, 2) NOT NULL CHECK (PackageWeightKg > 0),
    PackageVolumeM3 DECIMAL(10, 2) NOT NULL CHECK (PackageVolumeM3 > 0),
    Deadline DATETIME2 NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Pending',
    AssignedVehicleId INT NULL,
    AssignedDriverId INT NULL,
    CreatedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP,
    CreatedBy NVARCHAR(100) NOT NULL
);

CREATE INDEX idx_status ON Deliveries(Status);
CREATE INDEX idx_deadline ON Deliveries(Deadline);
CREATE INDEX idx_created_at ON Deliveries(CreatedAt);