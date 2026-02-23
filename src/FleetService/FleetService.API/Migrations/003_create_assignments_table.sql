CREATE TABLE Assignments (
    AssignmentId INT IDENTITY(1,1) PRIMARY KEY,
    DeliveryId INT NOT NULL,
    VehicleId INT NOT NULL,
    DriverId INT NOT NULL,
    AssignedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP,
    AssignedBy INT NOT NULL,
    CONSTRAINT FK_Assignments_Vehicles FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    CONSTRAINT FK_Assignments_Drivers FOREIGN KEY (DriverId) REFERENCES Drivers(DriverId)
);

CREATE INDEX idx_delivery_id ON Assignments(DeliveryId);
CREATE INDEX idx_vehicle_id ON Assignments(VehicleId);
CREATE INDEX idx_assigned_at ON Assignments(AssignedAt);
