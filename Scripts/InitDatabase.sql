CREATE DATABASE SiteSense;
GO
USE SiteSense;
GO
CREATE TABLE TelemetryPoints (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2(3) NOT NULL,
    VehicleId NVARCHAR(50) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Elevation FLOAT NOT NULL,
    VibrationFrequency FLOAT NOT NULL,
    CompactionValue FLOAT NOT NULL,
    IngestedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
