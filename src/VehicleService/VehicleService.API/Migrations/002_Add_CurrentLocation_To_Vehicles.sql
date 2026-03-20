IF COL_LENGTH('dbo.Vehicles', 'CurrentLocation') IS NULL
BEGIN
    ALTER TABLE Vehicles
    ADD CurrentLocation NVARCHAR(255) NOT NULL CONSTRAINT DF_Vehicles_CurrentLocation DEFAULT '';
END
