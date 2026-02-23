CREATE TABLE Drivers (
    DriverId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    FullName NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    LicenseNumber NVARCHAR(50) UNIQUE NOT NULL,
    MaxWorkingHoursPerDay INT DEFAULT 8 CHECK (MaxWorkingHoursPerDay BETWEEN 1 AND 12),
    Status NVARCHAR(50) DEFAULT 'Available',
    CreatedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_drivers_status ON Drivers(Status);
CREATE INDEX idx_user_id ON Drivers(UserId);
