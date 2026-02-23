CREATE TABLE RouteOptions (
    RouteOptionId INT IDENTITY(1,1) PRIMARY KEY,
    DeliveryId INT NOT NULL,
    Polyline NVARCHAR(MAX) NOT NULL,
    DistanceKm DECIMAL(10, 2) NOT NULL,
    DurationMinutes INT NOT NULL,
    EstimatedFuelCostLKR DECIMAL(10, 2) NOT NULL,
    IsSelected BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_delivery_id ON RouteOptions(DeliveryId);
