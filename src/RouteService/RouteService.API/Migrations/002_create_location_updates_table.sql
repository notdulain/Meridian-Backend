CREATE TABLE LocationUpdates (
    LocationUpdateId INT IDENTITY(1,1) PRIMARY KEY,
    DriverId INT NOT NULL,
    DeliveryId INT NULL,
    Latitude DECIMAL(10, 7) NOT NULL,
    Longitude DECIMAL(10, 7) NOT NULL,
    Timestamp DATETIME2 NOT NULL
);

CREATE INDEX idx_driver_id ON LocationUpdates(DriverId);
CREATE INDEX idx_delivery_id ON LocationUpdates(DeliveryId);
