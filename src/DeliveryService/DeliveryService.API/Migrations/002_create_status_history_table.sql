CREATE TABLE StatusHistory (
    StatusHistoryId INT IDENTITY(1,1) PRIMARY KEY,
    DeliveryId INT NOT NULL,
    PreviousStatus NVARCHAR(50),
    NewStatus NVARCHAR(50) NOT NULL,
    ChangedAt DATETIME2 DEFAULT CURRENT_TIMESTAMP,
    ChangedBy INT NOT NULL,
    Notes NVARCHAR(1000),
    CONSTRAINT FK_StatusHistory_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(DeliveryId) ON DELETE CASCADE
);

CREATE INDEX idx_delivery_id ON StatusHistory(DeliveryId);
